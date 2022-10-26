/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;
using System.IO;
using VSCodeDebug.Protocol;

namespace VSCodeDebug.Debugger;

// ---- Types -------------------------------------------------------------------------

public class Message
{
    public int id { get; }
    public string format { get; }
    public dynamic variables { get; }
    public dynamic showUser { get; }
    public dynamic sendTelemetry { get; }

    public Message(int id, string format, dynamic variables = null, bool user = true, bool telemetry = false)
    {
        this.id = id;
        this.format = format;
        this.variables = variables;
        showUser = user;
        sendTelemetry = telemetry;
    }
}

public class StackFrame
{
    public int id { get; }
    public Source source { get; }
    public int line { get; }
    public int column { get; }
    public string name { get; }
    public string presentationHint { get; }

    public StackFrame(int id, string name, Source source, int line, int column, string hint)
    {
        this.id = id;
        this.name = name;
        this.source = source;

        // These should NEVER be negative
        this.line = Math.Max(0, line);
        this.column = Math.Max(0, column);

        presentationHint = hint;
    }
}

public class Scope
{
    public string name { get; }
    public int variablesReference { get; }
    public bool expensive { get; }

    public Scope(string name, int variablesReference, bool expensive = false)
    {
        this.name = name;
        this.variablesReference = variablesReference;
        this.expensive = expensive;
    }
}

public class Variable
{
    public string name { get; }
    public string value { get; }
    public string type { get; }
    public int variablesReference { get; }

    public Variable(string name, string value, string type, int variablesReference = 0)
    {
        this.name = name;
        this.value = value;
        this.type = type;
        this.variablesReference = variablesReference;
    }
}

public class Thread
{
    public int id { get; }
    public string name { get; }

    public Thread(int id, string name)
    {
        this.id = id;
        if (name == null || name.Length == 0)
        {
            this.name = string.Format("Thread #{0}", id);
        }
        else
        {
            this.name = name;
        }
    }
}

public class Source
{
    public string name { get; }
    public string path { get; }
    public int sourceReference { get; }
    public string presentationHint { get; }

    public Source(string name, string path, int sourceReference, string hint)
    {
        this.name = name;
        this.path = path;
        this.sourceReference = sourceReference;
        presentationHint = hint;
    }
}

public class Breakpoint
{
    public bool verified { get; }
    public int line { get; }

    public Breakpoint(bool verified, int line)
    {
        this.verified = verified;
        this.line = line;
    }
}

// ---- The Session --------------------------------------------------------

public abstract class DebugSession : ProtocolServer
{
    private bool _clientLinesStartAt1 = true;
    private bool _clientPathsAreURI = true;


    public DebugSession()
    {
    }

    public void SendResponse(Response response, dynamic body = null)
    {
        if (body != null)
        {
            response.SetBody(body);
        }
        SendMessage(response);
    }

    public void SendErrorResponse(Response response, int id, string format, dynamic arguments = null, bool user = true, bool telemetry = false)
    {
        var msg = new Message(id, format, arguments, user, telemetry);
        var message = Utilities.ExpandVariables(msg.format, msg.variables);
        response.SetErrorBody(message, new ErrorResponseBody(msg));
        SendMessage(response);
    }

    protected override void DispatchRequest(Request request, Response response)
    {
        var command = request.command;
        var args = request.arguments;

		if (args == null)
        {
            args = new { };
        }

        try
        {
            switch (command)
            {

                case "initialize":
                    if (args.linesStartAt1 != null)
                    {
                        _clientLinesStartAt1 = (bool)args.linesStartAt1;
                    }
                    var pathFormat = (string)args.pathFormat;
                    if (pathFormat != null)
                    {
                        switch (pathFormat)
                        {
                            case "uri":
                                _clientPathsAreURI = true;
                                break;
                            case "path":
                                _clientPathsAreURI = false;
                                break;
                            default:
                                SendErrorResponse(response, 1015, "initialize: bad value '{_format}' for pathFormat", new { _format = pathFormat });
                                return;
                        }
                    }
                    Initialize(request, response);
                    break;

                case "launch":
                    Launch(request, response);
                    break;

                case "attach":
                    Attach(response, args);
                    break;

                case "disconnect":
                    Disconnect(request, response);
                    break;

                case "next":
                    Next(response, args);
                    break;

                case "continue":
                    Continue(response, args);
                    break;

                case "stepIn":
                    StepIn(response, args);
                    break;

                case "stepOut":
                    StepOut(response, args);
                    break;

                case "pause":
                    Pause(response, args);
                    break;

                case "stackTrace":
                    StackTrace(response, args);
                    break;

                case "scopes":
                    Scopes(response, args);
                    break;

                case "variables":
                    Variables(response, args);
                    break;

                case "source":
                    Source(response, args);
                    break;

                case "threads":
                    Threads(response, args);
                    break;

                case "setBreakpoints":
                    SetBreakpoints(response, args);
                    break;

                case "setFunctionBreakpoints":
                    SetFunctionBreakpoints(response, args);
                    break;

                case "setExceptionBreakpoints":
                    SetExceptionBreakpoints(response, args);
                    break;

                case "evaluate":
                    Evaluate(response, args);
                    break;

                default:
#if !EXCLUDE_HOT_RELOAD
                    if (HandleUnknownRequest?.Invoke((command, args, response)) ?? false)
                    {
                        //This was handled!
                        break;
                    }
#endif
                    SendErrorResponse(response, 1014, "unrecognized request: {_request}", new { _request = command });
                    break;
            }
        }
        catch (Exception e)
        {
            SendErrorResponse(response, 1104, "error while processing request '{_request}' (exception: {_exception})", new { _request = command, _exception = e.Message });
        }

        if (command == "disconnect")
        {
            Stop();
        }
    }

    public Func<(string command, dynamic args, Response response), bool> HandleUnknownRequest;

    public abstract void Initialize(Request request, Response response);

    public abstract void Launch(Request request, Response response);

    public abstract void Attach(Response response, dynamic arguments);

    public abstract void Disconnect(Request request, Response response);

    public virtual void SetFunctionBreakpoints(Response response, dynamic arguments)
    {
    }

    public virtual void SetExceptionBreakpoints(Response response, dynamic arguments)
    {
    }

    public abstract void SetBreakpoints(Response response, dynamic arguments);

    public abstract void Continue(Response response, dynamic arguments);

    public abstract void Next(Response response, dynamic arguments);

    public abstract void StepIn(Response response, dynamic arguments);

    public abstract void StepOut(Response response, dynamic arguments);

    public abstract void Pause(Response response, dynamic arguments);

    public abstract void StackTrace(Response response, dynamic arguments);

    public abstract void Scopes(Response response, dynamic arguments);

    public abstract void Variables(Response response, dynamic arguments);

    public abstract void Source(Response response, dynamic arguments);

    public abstract void Threads(Response response, dynamic arguments);

    public abstract void Evaluate(Response response, dynamic arguments);

    // protected

    protected int ConvertDebuggerLineToClient(int line)
    {
        return _clientLinesStartAt1 ? line : line - 1;
    }

    protected int ConvertClientLineToDebugger(int line)
    {
        return _clientLinesStartAt1 ? line : line + 1;
    }

    protected string ConvertDebuggerPathToClient(string path)
    {
        if (_clientPathsAreURI)
        {
            try
            {
                var uri = new Uri(path);
                return uri.AbsoluteUri;
            }
            catch
            {
                return null;
            }
        }
        else
        {
            return path;
        }
    }

    protected string ConvertClientPathToDebugger(string clientPath)
    {
        if (clientPath == null)
        {
            return null;
        }

        if (_clientPathsAreURI)
        {
            if (Uri.IsWellFormedUriString(clientPath, UriKind.Absolute))
            {
                Uri uri = new Uri(clientPath);
                return uri.LocalPath;
            }
            Program.Log("path not well formed: '{0}'", clientPath);
            return null;
        }
        else
        {
            return clientPath;
        }
    }
}
