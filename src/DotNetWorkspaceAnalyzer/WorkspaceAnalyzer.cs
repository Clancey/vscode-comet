using Microsoft.Build.Evaluation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace DotNetWorkspaceAnalyzer;

public partial class WorkspaceAnalyzer : IWorkspaceAnalyzerService
{
	ProjectCollection ProjectCollection = new ProjectCollection();

	public MSBuildWorkspace? CurrentWorkspace { get; private set; }

	string currentWorkspacePath = string.Empty;

	public string CurrentConfiguration { get; private set; } = "Debug";
	public string CurrentPlatform { get; private set; } = "AnyCPU";

	public async Task OpenWorkspace(string path, string? configuration, string? platform)
	{
		if (path.Equals(currentWorkspacePath))
			return;

		currentWorkspacePath = path;

		ProjectCollection.UnloadAllProjects();

		if (CurrentWorkspace is not null)
		{
			if (CurrentWorkspace.CurrentSolution is not null)
				CurrentWorkspace.CloseSolution();

			CurrentWorkspace.WorkspaceChanged -= CurrentWorkspace_WorkspaceChanged;
			CurrentWorkspace.WorkspaceFailed -= CurrentWorkspace_WorkspaceFailed;
		}

		
		CurrentConfiguration = configuration ?? "Debug";
		CurrentPlatform = platform ?? "AnyCPU";
		CurrentWorkspace = MSBuildWorkspace.Create(new Dictionary<string, string>()
		{
			["Configuration"] = CurrentConfiguration,
			["Platform"] = CurrentPlatform,
		});

		
		CurrentWorkspace.WorkspaceChanged += CurrentWorkspace_WorkspaceChanged;
		CurrentWorkspace.WorkspaceFailed += CurrentWorkspace_WorkspaceFailed;

		
		await CurrentWorkspace.OpenSolutionAsync(path);
	}

	private void CurrentWorkspace_WorkspaceFailed(object? sender, Microsoft.CodeAnalysis.WorkspaceDiagnosticEventArgs e)
	{
		// TODO: Notify failure
	}

	void DoThingsToProject(Solution solution, ProjectId? projectId, Action<Microsoft.Build.Evaluation.Project> thingToDo)
	{
		lock (parseLock)
		{
			var projects = solution.Projects.Where(p => p.Id.Equals(projectId));

			foreach (var rmp in projects)
			{
				foreach (var p in ProjectCollection.GetLoadedProjects(rmp.FilePath))
					thingToDo(p);
			}
		}
	}
	private async void CurrentWorkspace_WorkspaceChanged(object? sender, Microsoft.CodeAnalysis.WorkspaceChangeEventArgs e)
	{
		var hasUpdates = false;

		if (e.Kind == WorkspaceChangeKind.ProjectAdded)
		{
			var newProjectId = e.ProjectId;
			foreach (var p in e.NewSolution.Projects.Where(p => p.Id.Equals(newProjectId)))
				ProjectCollection.LoadProject(p.FilePath);
			
			hasUpdates = true;
		}
		else if (e.Kind == WorkspaceChangeKind.ProjectRemoved)
		{
			DoThingsToProject(
				e.NewSolution,
				e.ProjectId,
				p => ProjectCollection.TryUnloadProject(p.Xml));

			hasUpdates = true;
		}
		else if (e.Kind == WorkspaceChangeKind.ProjectReloaded)
		{
			var projectId = e.ProjectId;
			var projects = e.NewSolution.Projects.Where(p => p.Id.Equals(projectId));

			foreach (var rmp in projects)
			{
				foreach (var p in ProjectCollection.GetLoadedProjects(rmp.FilePath))
					p.ReevaluateIfNecessary();
			}

			hasUpdates = true;
		}
		else if (e.Kind == WorkspaceChangeKind.ProjectChanged)
		{
			DoThingsToProject(
				e.NewSolution,
				e.ProjectId,
				p => p.ReevaluateIfNecessary());

			hasUpdates = true;
		}
		else if (e.Kind == WorkspaceChangeKind.SolutionReloaded
			|| e.Kind == WorkspaceChangeKind.SolutionChanged
			|| e.Kind == WorkspaceChangeKind.SolutionAdded
			|| e.Kind == WorkspaceChangeKind.SolutionCleared)
		{
			hasUpdates = true;
		}

		if (!hasUpdates)
			return;
		
		var sln = CurrentWorkspace?.CurrentSolution ?? e.NewSolution;
		var slnProjects = sln?.Projects ?? Enumerable.Empty<Microsoft.CodeAnalysis.Project>();
		
		var projectInfos = new List<ProjectInfo>();

		foreach (var p in slnProjects.GroupBy(p => p.FilePath))
		{
			projectInfos.Add(ParseProject(p.Key, false, new Dictionary<string, string>
			{
				["Configuration"] = CurrentConfiguration,
				["Platform"] = CurrentPlatform
			}));
		}

		WorkspaceChanged?.Invoke(this, new WorkspaceInfo
		{
			Solution = new SolutionInfo
			{
				Projects = projectInfos.ToArray()
			}
		});
	}

	public Task<string> Helo()
	{
		return Task.FromResult("ehlo");
	}

	public event EventHandler<WorkspaceInfo> WorkspaceChanged;


	public Task<ProjectInfo> EvaluateProject(string projectFile, IDictionary<string, string> properties)
	{
		var projectInfo = ParseProject(projectFile, true, properties);

		return Task.FromResult(projectInfo);
	}

	static readonly object parseLock = new();

	ProjectInfo ParseProject(string projectFile, bool reevaluate, IDictionary<string, string> properties)
	{
		lock (parseLock)
		{
			ProjectCollection pc;

			if (!properties.ContainsKey("Configuration"))
				properties["Configuration"] = CurrentConfiguration;
			if (!properties.ContainsKey("Platform"))
				properties["Platform"] = CurrentPlatform;

			var msbuildProject = ProjectCollection.GetLoadedProjects(projectFile)?.FirstOrDefault();

			if (msbuildProject is null)
				msbuildProject = ProjectCollection.LoadProject(projectFile, properties, null);

			// Update the properties
			foreach (var prop in properties)
				msbuildProject.SetGlobalProperty(prop.Key, prop.Value);

			if (reevaluate)
				msbuildProject.ReevaluateIfNecessary();

			var asmName = msbuildProject.GetPropertyValue("AssemblyName");

			var isExe = msbuildProject.GetPropertyValue("OutputType")?.Equals("exe", StringComparison.OrdinalIgnoreCase) ?? false;
			var tfms = msbuildProject.GetPropertyValue("TargetFrameworks");
			if (string.IsNullOrEmpty(tfms))
				tfms = msbuildProject.GetPropertyValue("TargetFramework");

			var configs = msbuildProject.GetPropertyValue("Configurations");
			if (string.IsNullOrEmpty(configs))
				configs = msbuildProject.GetPropertyValue("Configuration");

			var plats = msbuildProject.GetPropertyValue("Platforms");
			if (string.IsNullOrEmpty(plats))
				plats = msbuildProject.GetPropertyValue("Platform");

			var tfmInfos = new List<TargetFrameworkInfo>();

			foreach (var tfmstr in tfms.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			{
				var nfw = NuGet.Frameworks.NuGetFramework.Parse(tfmstr);
				tfmInfos.Add(new TargetFrameworkInfo
				{
					FullName = tfmstr,
					Platform = nfw.Platform,
					Version = nfw.Version.ToString(),
					PlatformVersion = nfw.PlatformVersion == new Version() ? null : nfw.PlatformVersion.ToString()
				});
			}

			var props = new Dictionary<string, string>();
			foreach (var p in msbuildProject.AllEvaluatedProperties)
				props[p.Name] = p.EvaluatedValue;

			return new ProjectInfo
			{
				IsExe = isExe,
				Name = asmName,
				Path = msbuildProject.FullPath ?? string.Empty,
				Platforms = plats.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
				Configurations = configs.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
				TargetFrameworks = tfmInfos.ToArray(),
				OutputPath = msbuildProject.GetPropertyValue("OutputPath"),
				Properties = props
			};
		}
	}
}
