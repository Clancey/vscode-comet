namespace DotNetWorkspaceAnalyzer;

interface IWorkspaceAnalyzerService
{
    Task<string> Helo();

    event EventHandler<WorkspaceInfo> WorkspaceChanged;

    Task OpenWorkspace(string path, string configuration = "Debug", string platform = "AnyCPU");
}
