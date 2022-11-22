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

	public string OutputPath { get;set; }

	public IReadOnlyDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
}

public class TargetFrameworkInfo
{
	public string FullName { get; set; } = "net7.0";
	public string? Platform { get; set; }
	public string Version { get; set; } = "7.0.0.0";
	public string? PlatformVersion { get; set; }
}
