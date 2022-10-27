using System.Diagnostics.Tracing;
using DotNetWorkspaceAnalyzer;
using StreamJsonRpc;


WorkspaceAnalyzer? service;

Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults();

service = new WorkspaceAnalyzer();

var rpc = JsonRpc.Attach(Console.OpenStandardOutput(), Console.OpenStandardInput(), service);

await rpc.Completion;
