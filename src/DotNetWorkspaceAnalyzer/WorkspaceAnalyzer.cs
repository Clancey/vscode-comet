using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace DotNetWorkspaceAnalyzer;

public partial class WorkspaceAnalyzer : IWorkspaceAnalyzerService
{
    
    public MSBuildWorkspace? CurrentWorkspace { get; private set; }
    public Solution? CurrentSolution { get; private set; }

    public string CurrentConfiguration { get; private set; } = "Debug";
    public string CurrentPlatform { get; private set; } = "AnyCPU";

    public async Task OpenWorkspace(string path, string? configuration, string? platform)
    {
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

        CurrentSolution = await CurrentWorkspace.OpenSolutionAsync(path);
    }

    private void CurrentWorkspace_WorkspaceFailed(object? sender, Microsoft.CodeAnalysis.WorkspaceDiagnosticEventArgs e)
    {
        // TODO: Notify failure
    }

    private async void CurrentWorkspace_WorkspaceChanged(object? sender, Microsoft.CodeAnalysis.WorkspaceChangeEventArgs e)
    {
        // TODO: Notify change
        //if (!(e.Kind == Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectAdded
        //    || e.Kind == Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectChanged
        //    || e.Kind == Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectReloaded
        //    || e.Kind == Microsoft.CodeAnalysis.WorkspaceChangeKind.ProjectRemoved
        //    || e.Kind == Microsoft.CodeAnalysis.WorkspaceChangeKind.SolutionAdded
        //    || e.Kind == Microsoft.CodeAnalysis.WorkspaceChangeKind.SolutionChanged
        //    || e.Kind == Microsoft.CodeAnalysis.WorkspaceChangeKind.SolutionCleared
        //    || e.Kind == Microsoft.CodeAnalysis.WorkspaceChangeKind.SolutionReloaded
        //    || e.Kind == Microsoft.CodeAnalysis.WorkspaceChangeKind.SolutionRemoved))
        //    return;

        if (string.IsNullOrEmpty(e.NewSolution?.FilePath))
            return;

        var projectInfos = new List<ProjectInfo>();

        var slnProjects = e.NewSolution?.Projects ?? Enumerable.Empty<Project>();
        
        foreach (var prj in slnProjects.GroupBy(p => p.FilePath))
        {
            var msbuildProject = new Microsoft.Build.Evaluation.Project(prj.First().FilePath,new Dictionary<string, string>
            {
                {  "Configuration", CurrentConfiguration },
                {  "Platform", CurrentPlatform}
            }, null);

            
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
            
            projectInfos.Add(new ProjectInfo
            {
                IsExe = isExe,
                Name = asmName,
                Path = msbuildProject.FullPath ?? string.Empty,
                Platforms = plats.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                Configurations = configs.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                TargetFrameworks = tfmInfos.ToArray()
            });
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
}
