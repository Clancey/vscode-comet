/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Linq;
using System.Net;
using VsCodeMobileUtil;
using Mono.Debugging.Client;
using System.Diagnostics.CodeAnalysis;
using VSCodeDebug.Protocol;
#if !EXCLUDE_HOT_RELOAD
using VSCodeDebug.HotReload;
#endif
using System.Threading.Tasks;

namespace VSCodeDebug.Debugger;

public class MonoDebugSession : DebugSession
{
	private const string MONO = "mono";
	private readonly string[] MONO_EXTENSIONS = new string[] {
		".cs", ".csx",
		".cake",
		".fs", ".fsi", ".ml", ".mli", ".fsx", ".fsscript",
		".hx"
	};
	private const int MAX_CHILDREN = 100;
	private const int MAX_CONNECTION_ATTEMPTS = 20;
	private const int CONNECTION_ATTEMPT_INTERVAL = 500;

	private AutoResetEvent _resumeEvent = new AutoResetEvent(false);
	private bool _debuggeeExecuting = false;
	private readonly object _lock = new object();
	private Mono.Debugging.Soft.SoftDebuggerSession _session;
	private volatile bool _debuggeeKilled = true;
	private ProcessInfo _activeProcess;
	private Mono.Debugging.Client.StackFrame _activeFrame;
	private long _nextBreakpointId = 0;
	private SortedDictionary<long, BreakEvent> _breakpoints;
	private List<Catchpoint> _catchpoints;
	private DebuggerSessionOptions _debuggerSessionOptions;

	private System.Diagnostics.Process _process;
	private Handles<ObjectValue[]> _variableHandles;
	private Handles<Mono.Debugging.Client.StackFrame> _frameHandles;
	private ObjectValue _exception;
	private Dictionary<int, Thread> _seenThreads = new Dictionary<int, Thread>();
	protected bool AttachMode { get; set; } = false;
	private bool _terminated = false;
	private bool _stderrEOF = true;
	private bool _stdoutEOF = true;

	protected HotReloadManager HotReloadManager;

	public MonoDebugSession() : base()
	{
		_variableHandles = new Handles<ObjectValue[]>();
		_frameHandles = new Handles<Mono.Debugging.Client.StackFrame>();
		_seenThreads = new Dictionary<int, Thread>();
		HotReloadManager ??= new HotReloadManager();
		HotReloadManager.OutputHandler = SendConsoleEvent;
		_debuggerSessionOptions = new DebuggerSessionOptions
		{
			EvaluationOptions = EvaluationOptions.DefaultOptions
		};

		_session = new Mono.Debugging.Soft.SoftDebuggerSession();
		_session.Breakpoints = new BreakpointStore();

		_breakpoints = new SortedDictionary<long, BreakEvent>();
		_catchpoints = new List<Catchpoint>();

		DebuggerLoggingService.CustomLogger = new CustomLogger();

		_session.ExceptionHandler = ex =>
		{
			return true;
		};

		_session.LogWriter = (isStdErr, text) =>
		{
		};

		_session.TargetStopped += (sender, e) =>
		{
			Stopped();
			SendEvent(CreateStoppedEvent("step", e.Thread));
			_resumeEvent.Set();
		};

		_session.TargetHitBreakpoint += (sender, e) =>
		{
			Stopped();
			SendEvent(CreateStoppedEvent("breakpoint", e.Thread));
			_resumeEvent.Set();
		};

		_session.TargetExceptionThrown += (sender, e) =>
		{
			Stopped();
			var ex = DebuggerActiveException();
			if (ex != null)
			{
				_exception = ex.Instance;
				SendEvent(CreateStoppedEvent("exception", e.Thread, ex.Message));
			}
			_resumeEvent.Set();
		};

		_session.TargetUnhandledException += (sender, e) =>
		{
			Stopped();
			var ex = DebuggerActiveException();
			if (ex != null)
			{
				_exception = ex.Instance;
				SendEvent(CreateStoppedEvent("exception", e.Thread, ex.Message));
			}
			_resumeEvent.Set();
		};

		_session.TargetStarted += (sender, e) =>
		{
			_activeFrame = null;
		};

		_session.TargetReady += (sender, e) =>
		{
			_activeProcess = _session.GetProcesses().SingleOrDefault();

			HotReloadManager?.Start(_session);
		};

		_session.TargetExited += (sender, e) =>
		{

			HotReloadManager?.Stop();
			DebuggerKill();

			_debuggeeKilled = true;

			Terminate("target exited");

			_resumeEvent.Set();
		};

		_session.TargetInterrupted += (sender, e) =>
		{
			_resumeEvent.Set();
		};

		_session.TargetEvent += (sender, e) =>
		{
		};

		_session.TargetThreadStarted += (sender, e) =>
		{
			int tid = (int)e.Thread.Id;
			lock (_seenThreads)
			{
				_seenThreads[tid] = new Thread(tid, e.Thread.Name);
			}
			SendEvent(new ThreadEvent("started", tid));
		};

		_session.TargetThreadStopped += (sender, e) =>
		{
			int tid = (int)e.Thread.Id;
			lock (_seenThreads)
			{
				_seenThreads.Remove(tid);
			}
			SendEvent(new ThreadEvent("exited", tid));
		};

		_session.OutputWriter = (isStdErr, text) =>
		{
			SendOutput(isStdErr ? "stderr" : "stdout", text);
		};

		this.HandleUnknownRequest = (s) =>
		{
			if (s.command == "DocumentChanged")
			{
				if (HotReloadManager == null)
					return false;

				string fullPath = s.args.fullPath;
				string relativePath = s.args.relativePath;
				HotReloadManager.DocumentChanged(fullPath, relativePath);
				return true;
			}
			return false;
		};
	}

	public override void Initialize(Request request, Response response)
	{
		SendResponse(response, new Capabilities()
		{
			// This debug adapter does not need the configurationDoneRequest.
			supportsConfigurationDoneRequest = false,

			// This debug adapter does not support function breakpoints.
			supportsFunctionBreakpoints = false,

			// This debug adapter doesn't support conditional breakpoints.
			supportsConditionalBreakpoints = false,

			// This debug adapter does not support a side effect free evaluate request for data hovers.
			supportsEvaluateForHovers = true,

			// This debug adapter does not support exception breakpoint filters
			exceptionBreakpointFilters = new dynamic[0]
		});

		// Mono Debug is ready to accept breakpoints immediately
		SendEvent(new InitializedEvent());
	}

	public override async void Launch(Request request, Response response)
	{
		AttachMode = false;

		SetExceptionBreakpoints(request.arguments.__exceptionOptions);

		var launchOptions = new LaunchData(request.arguments);
		var valid = launchOptions.Validate();
		HotReloadManager?.SetLaunchData(launchOptions);

		if (!valid.success)
		{
			SendErrorResponse(response, 3002, valid.message);
			return;
		}

		int port = launchOptions.DebugPort;
		var host = GetString(request.arguments, "address");
		IPAddress address = string.IsNullOrWhiteSpace(host) ? IPAddress.Loopback : Utilities.ResolveIPAddress(host);
		if (address == null)
		{
			SendErrorResponse(response, 3013, "Invalid address '{address}'.", new { address = address });
			return;
		}
		Connect(launchOptions, address, port);

		SendResponse(response);
	}



	protected void Connect(LaunchData options, IPAddress address, int port)
	{
		lock (_lock)
		{
			_debuggeeKilled = false;

			Mono.Debugging.Soft.SoftDebuggerStartArgs args = null;
			if (options.ProjectType == ProjectType.Android)
			{
				args = new Mono.Debugging.Soft.SoftDebuggerConnectArgs(options.AppName, address, port)
				{
					MaxConnectionAttempts = MAX_CONNECTION_ATTEMPTS,
					TimeBetweenConnectionAttempts = CONNECTION_ATTEMPT_INTERVAL
				};
			}
			else if (options.ProjectType == ProjectType.iOS || options.ProjectType == ProjectType.MacCatalyst)
			{
				args = new StreamCommandConnectionDebuggerArgs(options.AppName, new IPhoneTcpCommandConnection(IPAddress.Loopback, port)) { MaxConnectionAttempts = 10 };
			}

			SendConsoleEvent($"Debugger is ready and listening...");

			_debuggeeExecuting = true;
			_session.Run(new Mono.Debugging.Soft.SoftDebuggerStartInfo(args), _debuggerSessionOptions);
		}
	}

	public override void Attach(Response response, dynamic args)
	{
		AttachMode = true;

		SetExceptionBreakpoints(args.__exceptionOptions);

		// validate argument 'address'
		var host = GetString(args, "address");
		if (host == null)
		{
			SendErrorResponse(response, 3007, "Property 'address' is missing or empty.");
			return;
		}

		// validate argument 'port'
		var port = GetInt(args, "port", -1);
		if (port == -1)
		{
			SendErrorResponse(response, 3008, "Property 'port' is missing.");
			return;
		}

		IPAddress address = Utilities.ResolveIPAddress(host);
		if (address == null)
		{
			SendErrorResponse(response, 3013, "Invalid address '{address}'.", new { address });
			return;
		}

		Connect(address, port);

		SendResponse(response);
	}

	public override void Disconnect(Request request, Response response)
	{
		if (AttachMode)
		{

			lock (_lock)
			{
				if (_session != null)
				{
					_debuggeeExecuting = true;
					_breakpoints.Clear();
					_session.Breakpoints.Clear();
					_session.Continue();
					_session = null;
				}
			}

		}
		else
		{
			// Let's not leave dead Mono processes behind...
			if (_process != null)
			{
				_process.Kill();
				_process = null;
			}
			else
			{
				PauseDebugger();
				DebuggerKill();

				while (!_debuggeeKilled)
				{
					System.Threading.Thread.Sleep(10);
				}
			}
		}

		SendResponse(response);
	}

	public void SendConsoleEvent(string message)
	{
		Console.WriteLine(message);
		SendEvent(new ConsoleOutputEvent(message.TrimEnd() + Environment.NewLine));
	}

	public override void Continue(Response response, dynamic args)
	{
		WaitForSuspend();
		SendResponse(response);
		lock (_lock)
		{
			if (_session != null && !_session.IsRunning && !_session.HasExited)
			{
				_session.Continue();
				_debuggeeExecuting = true;
			}
		}
	}

	public override void Next(Response response, dynamic args)
	{
		WaitForSuspend();
		SendResponse(response);
		lock (_lock)
		{
			if (_session != null && !_session.IsRunning && !_session.HasExited)
			{
				_session.NextLine();
				_debuggeeExecuting = true;
			}
		}
	}

	public override void StepIn(Response response, dynamic args)
	{
		WaitForSuspend();
		SendResponse(response);
		lock (_lock)
		{
			if (_session != null && !_session.IsRunning && !_session.HasExited)
			{
				_session.StepLine();
				_debuggeeExecuting = true;
			}
		}
	}

	public override void StepOut(Response response, dynamic args)
	{
		WaitForSuspend();
		SendResponse(response);
		lock (_lock)
		{
			if (_session != null && !_session.IsRunning && !_session.HasExited)
			{
				_session.Finish();
				_debuggeeExecuting = true;
			}
		}
	}

	public override void Pause(Response response, dynamic args)
	{
		SendResponse(response);
		PauseDebugger();
	}

	public override void SetExceptionBreakpoints(Response response, dynamic args)
	{
		SetExceptionBreakpoints(args.exceptionOptions);
		SendResponse(response);
	}

	public override void SetBreakpoints(Response response, dynamic args)
	{
		string path = null;
		if (args.source != null)
		{
			string p = (string)args.source.path;
			if (p != null && p.Trim().Length > 0)
			{
				path = p;
			}
		}
		if (path == null)
		{
			SendErrorResponse(response, 3010, "setBreakpoints: property 'source' is empty or misformed", null, false, true);
			return;
		}
		path = ConvertClientPathToDebugger(path);

		if (!HasMonoExtension(path))
		{
			// we only support breakpoints in files mono can handle
			SendResponse(response, new SetBreakpointsResponseBody());
			return;
		}

		var clientLines = args.lines.ToObject<int[]>();
		HashSet<int> lin = new HashSet<int>();
		for (int i = 0; i < clientLines.Length; i++)
		{
			lin.Add(ConvertClientLineToDebugger(clientLines[i]));
		}

		// find all breakpoints for the given path and remember their id and line number
		var bpts = new List<Tuple<int, int>>();
		foreach (var be in _breakpoints)
		{
			var bp = be.Value as Mono.Debugging.Client.Breakpoint;
			if (bp != null && bp.FileName == path)
			{
				bpts.Add(new Tuple<int, int>((int)be.Key, bp.Line));
			}
		}

		HashSet<int> lin2 = new HashSet<int>();
		foreach (var bpt in bpts)
		{
			if (lin.Contains(bpt.Item2))
			{
				lin2.Add(bpt.Item2);
			}
			else
			{
				// Program.Log("cleared bpt #{0} for line {1}", bpt.Item1, bpt.Item2);

				BreakEvent b;
				if (_breakpoints.TryGetValue(bpt.Item1, out b))
				{
					_breakpoints.Remove(bpt.Item1);
					_session.Breakpoints.Remove(b);
				}
			}
		}

		for (int i = 0; i < clientLines.Length; i++)
		{
			var l = ConvertClientLineToDebugger(clientLines[i]);
			if (!lin2.Contains(l))
			{
				var id = _nextBreakpointId++;
				_breakpoints.Add(id, _session.Breakpoints.Add(path, l));
				// Program.Log("added bpt #{0} for line {1}", id, l);
			}
		}

		var breakpoints = new List<Breakpoint>();
		foreach (var l in clientLines)
		{
			breakpoints.Add(new Breakpoint(true, l));
		}

		SendResponse(response, new SetBreakpointsResponseBody(breakpoints));
	}

	public override void StackTrace(Response response, dynamic args)
	{
		// HOT RELOAD: Seems that sometimes there's a hang here, look out for this in the future
		// TODO: Getting a stack trace can hang; we need to fix it but for now just return an empty one
		//SendResponse(response, new StackTraceResponseBody(new List<StackFrame>(), 0));
		//return;

		int maxLevels = GetInt(args, "levels", 10);
		int threadReference = GetInt(args, "threadId", 0);

		WaitForSuspend();

		ThreadInfo thread = DebuggerActiveThread();
		if (thread.Id != threadReference)
		{
			// Program.Log("stackTrace: unexpected: active thread should be the one requested");
			thread = FindThread(threadReference);
			if (thread != null)
			{
				thread.SetActive();
			}
		}

		var stackFrames = new List<StackFrame>();
		int totalFrames = 0;

		var bt = thread.Backtrace;
		if (bt != null && bt.FrameCount >= 0)
		{

			totalFrames = bt.FrameCount;

			for (var i = 0; i < Math.Min(totalFrames, maxLevels); i++)
			{

				var frame = bt.GetFrame(i);

				string path = frame.SourceLocation.FileName;

				var hint = "subtle";
				Source source = null;
				if (!string.IsNullOrEmpty(path))
				{
					string sourceName = Path.GetFileName(path);
					if (!string.IsNullOrEmpty(sourceName))
					{
						if (File.Exists(path))
						{
							source = new Source(sourceName, ConvertDebuggerPathToClient(path), 0, "normal");
							hint = "normal";
						}
						else
						{
							source = new Source(sourceName, null, 1000, "deemphasize");
						}
					}
				}

				var frameHandle = _frameHandles.Create(frame);
				string name = frame.SourceLocation.MethodName;
				int line = frame.SourceLocation.Line;
				stackFrames.Add(new StackFrame(frameHandle, name, source, ConvertDebuggerLineToClient(line), 0, hint));
			}
		}

		SendResponse(response, new StackTraceResponseBody(stackFrames, totalFrames));
	}

	public override void Source(Response response, dynamic arguments)
	{
		SendErrorResponse(response, 1020, "No source available");
	}

	public override void Scopes(Response response, dynamic args)
	{

		int frameId = GetInt(args, "frameId", 0);
		var frame = _frameHandles.Get(frameId, null);

		var scopes = new List<Scope>();

		// TODO: I'm not sure if this is the best response in this scenario but it at least avoids an NRE
		if (frame == null)
		{
			SendResponse(response, new ScopesResponseBody(scopes));
			return;
		}

		if (frame.Index == 0 && _exception != null)
		{
			scopes.Add(new Scope("Exception", _variableHandles.Create(new ObjectValue[] { _exception })));
		}

		var paramObs = frame.GetParameters();
		if (paramObs != null && paramObs.Length > 0)
			scopes.Add(new Scope("Arguments", _variableHandles.Create(paramObs)));


		var allLocalsObjs = frame.GetAllLocals();
		if (paramObs != null && paramObs.Length > 0)
			scopes.Add(new Scope("Locals", _variableHandles.Create(allLocalsObjs)));

		SendResponse(response, new ScopesResponseBody(scopes));
	}

	public override void Variables(Response response, dynamic args)
	{
		int reference = GetInt(args, "variablesReference", -1);
		if (reference == -1)
		{
			SendErrorResponse(response, 3009, "variables: property 'variablesReference' is missing", null, false, true);
			return;
		}

		WaitForSuspend();
		var variables = new List<Variable>();

		ObjectValue[] children;
		if (_variableHandles.TryGet(reference, out children))
		{
			if (children != null && children.Length > 0)
			{

				bool more = false;
				if (children.Length > MAX_CHILDREN)
				{
					children = children.Take(MAX_CHILDREN).ToArray();
					more = true;
				}

				if (children.Length < 20)
				{
					// Wait for all values at once.
					WaitHandle.WaitAll(children.Select(x => x.WaitHandle).ToArray());
					foreach (var v in children)
					{
						variables.Add(CreateVariable(v));
					}
				}
				else
				{
					foreach (var v in children)
					{
						v.WaitHandle.WaitOne();
						variables.Add(CreateVariable(v));
					}
				}

				if (more)
				{
					variables.Add(new Variable("...", null, null));
				}
			}
		}

		SendResponse(response, new VariablesResponseBody(variables));
	}

	public override void Threads(Response response, dynamic args)
	{
		var threads = new List<Thread>();
		var process = _activeProcess;
		if (process != null)
		{
			Dictionary<int, Thread> d;
			lock (_seenThreads)
			{
				d = new Dictionary<int, Thread>(_seenThreads);
			}
			foreach (var t in process.GetThreads())
			{
				int tid = (int)t.Id;
				d[tid] = new Thread(tid, t.Name);
			}
			threads = d.Values.ToList();
		}
		SendResponse(response, new ThreadsResponseBody(threads));
	}

	public override void Evaluate(Response response, dynamic args)
	{
		string error = null;

		var expression = GetString(args, "expression");
		if (expression == null)
		{
			error = "expression missing";
		}
		else
		{
			int frameId = GetInt(args, "frameId", -1);
			var frame = _frameHandles.Get(frameId, null);
			if (frame != null)
			{
				if (frame.ValidateExpression(expression))
				{
					var val = frame.GetExpressionValue(expression, _debuggerSessionOptions.EvaluationOptions);
					val.WaitHandle.WaitOne();

					var flags = val.Flags;
					if (flags.HasFlag(ObjectValueFlags.Error) || flags.HasFlag(ObjectValueFlags.NotSupported))
					{
						error = val.DisplayValue;

						SendResponse(response, new EvaluateResponseBody(expression));
						return;
					}
					else if (flags.HasFlag(ObjectValueFlags.Unknown))
					{
						error = "invalid expression";
						SendResponse(response, new EvaluateResponseBody(expression));
						return;
					}
					else if (flags.HasFlag(ObjectValueFlags.Object) && flags.HasFlag(ObjectValueFlags.Namespace))
					{
						error = "not available";
						SendResponse(response, new EvaluateResponseBody(expression));
						return;
					}
					else
					{
						int handle = 0;
						if (val.HasChildren)
						{
							handle = _variableHandles.Create(val.GetAllChildren());
						}
						SendResponse(response, new EvaluateResponseBody(val.DisplayValue, handle, val.TypeName,
							new VariablePresentationHint("property", null)));
						return;
					}
				}
				else
				{
					error = "invalid expression";
					SendResponse(response, new EvaluateResponseBody(expression));
					return;
				}
			}
			else
			{
				error = "no active stackframe";
			}
		}
		SendErrorResponse(response, 3014, "Evaluate request failed ({_reason}).", new { _reason = error });
	}

	//---- private ------------------------------------------

	protected void SetExceptionBreakpoints(dynamic exceptionOptions)
	{
		if (exceptionOptions != null)
		{

			// clear all existig catchpoints
			foreach (var cp in _catchpoints)
			{
				_session.Breakpoints.Remove(cp);
			}
			_catchpoints.Clear();

			var exceptions = exceptionOptions.ToObject<dynamic[]>();
			for (int i = 0; i < exceptions.Length; i++)
			{

				var exception = exceptions[i];

				string exName = null;
				string exBreakMode = exception.breakMode;

				if (exception.path != null)
				{
					var paths = exception.path.ToObject<dynamic[]>();
					var path = paths[0];
					if (path.names != null)
					{
						var names = path.names.ToObject<dynamic[]>();
						if (names.Length > 0)
						{
							exName = names[0];
						}
					}
				}

				if (exName != null && exBreakMode == "always")
				{
					_catchpoints.Add(_session.Breakpoints.AddCatchpoint(exName));
				}
			}
		}
	}

	private void SendOutput(string category, string data)
	{
		if (!string.IsNullOrEmpty(data))
		{
			if (data[data.Length - 1] != '\n')
			{
				data += '\n';
			}
			SendEvent(new OutputEvent(category, data));
		}
	}

	private void Terminate(string reason)
	{
		if (!_terminated)
		{

			// wait until we've seen the end of stdout and stderr
			for (int i = 0; i < 100 && (_stdoutEOF == false || _stderrEOF == false); i++)
			{
				System.Threading.Thread.Sleep(100);
			}

			SendEvent(new TerminatedEvent());

			_terminated = true;
			_process = null;
		}
	}

	private StoppedEvent CreateStoppedEvent(string reason, ThreadInfo ti, string text = null)
	{
		return new StoppedEvent((int)ti.Id, reason, text);
	}

	private ThreadInfo FindThread(int threadReference)
	{
		if (_activeProcess != null)
		{
			foreach (var t in _activeProcess.GetThreads())
			{
				if (t.Id == threadReference)
				{
					return t;
				}
			}
		}
		return null;
	}

	private void Stopped()
	{
		_exception = null;
		_variableHandles.Reset();
		_frameHandles.Reset();
	}

	private Variable CreateVariable(ObjectValue v)
	{
		var dv = v.DisplayValue;
		if (dv == null)
		{
			dv = "<error getting value>";
		}

		if (dv.Length > 1 && dv[0] == '{' && dv[dv.Length - 1] == '}')
		{
			dv = dv.Substring(1, dv.Length - 2);
		}
		return new Variable(v.Name, dv, v.TypeName, v.HasChildren ? _variableHandles.Create(v.GetAllChildren()) : 0);
	}

	private bool HasMonoExtension(string path)
	{
		foreach (var e in MONO_EXTENSIONS)
		{
			if (path.EndsWith(e))
			{
				return true;
			}
		}
		return false;
	}

	protected static bool GetBool(dynamic container, string propertyName, bool dflt = false)
	{
		try
		{
			return (bool)container[propertyName];
		}
		catch (Exception)
		{
			// ignore and return default value
		}
		return dflt;
	}

	protected static int GetInt(dynamic container, string propertyName, int dflt = 0)
	{
		try
		{
			return (int)container[propertyName];
		}
		catch (Exception)
		{
			// ignore and return default value
		}
		return dflt;
	}

	protected static string GetString(dynamic args, string property, string dflt = null)
	{
		var s = (string)args[property];
		if (s == null)
		{
			return dflt;
		}
		s = s.Trim();
		if (s.Length == 0)
		{
			return dflt;
		}
		return s;
	}

	//-----------------------

	private void WaitForSuspend()
	{
		if (_debuggeeExecuting)
		{
			_resumeEvent.WaitOne();
			_debuggeeExecuting = false;
		}
	}

	private ThreadInfo DebuggerActiveThread()
	{
		lock (_lock)
		{
			return _session == null ? null : _session.ActiveThread;
		}
	}

	private Backtrace DebuggerActiveBacktrace()
	{
		var thr = DebuggerActiveThread();
		return thr == null ? null : thr.Backtrace;
	}

	private Mono.Debugging.Client.StackFrame DebuggerActiveFrame()
	{
		if (_activeFrame != null)
			return _activeFrame;

		var bt = DebuggerActiveBacktrace();
		if (bt != null)
			return _activeFrame = bt.GetFrame(0);

		return null;
	}

	private ExceptionInfo DebuggerActiveException()
	{
		var bt = DebuggerActiveBacktrace();
		return bt == null ? null : bt.GetFrame(0).GetException();
	}

	private void Connect(IPAddress address, int port)
	{
		lock (_lock)
		{

			_debuggeeKilled = false;

			var args0 = new Mono.Debugging.Soft.SoftDebuggerConnectArgs(string.Empty, address, port)
			{
				MaxConnectionAttempts = MAX_CONNECTION_ATTEMPTS,
				TimeBetweenConnectionAttempts = CONNECTION_ATTEMPT_INTERVAL
			};

			_session.Run(new Mono.Debugging.Soft.SoftDebuggerStartInfo(args0), _debuggerSessionOptions);

			_debuggeeExecuting = true;
		}
	}

	private void PauseDebugger()
	{
		lock (_lock)
		{
			if (_session != null && _session.IsRunning)
				_session.Stop();
		}
	}

	private void DebuggerKill()
	{
		lock (_lock)
		{
			if (_session != null)
			{

				_debuggeeExecuting = true;

				if (!_session.HasExited)
					_session.Exit();

				_session.Dispose();
				_session = null;
			}
		}
	}
}
