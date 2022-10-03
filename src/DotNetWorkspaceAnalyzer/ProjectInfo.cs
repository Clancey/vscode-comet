namespace DotNetWorkspaceAnalyzer;

public class ProjectInfo
{
    public string Name { get; set; }
    public string ProjectGuid { get; set; }
    public string Path { get; set; }

	public TargetFrameworkInfo[] TargetFrameworks { get; set; }

    public bool IsExe { get; set; }
    
	public string[] Configurations { get; set; }
    public string[] Platforms { get; set; }
}

public class TargetFrameworkInfo
{
    public string FullName { get; set; } = "net6.0";
    public string? Platform { get; set; }
    public string Version { get; set; } = "6.0.0.0";
    public string? PlatformVersion { get; set; }
}
