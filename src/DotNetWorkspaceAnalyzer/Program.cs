using System.Diagnostics.Tracing;
using DotNetWorkspaceAnalyzer;
using StreamJsonRpc;


WorkspaceAnalyzer? service;

Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults();

service = new WorkspaceAnalyzer();

await service.OpenWorkspace("/Users/redth/Desktop/MauiTest/MauiTest.sln", "Debug", "AnyCPU");

var rpc = JsonRpc.Attach(Console.OpenStandardOutput(), Console.OpenStandardInput(), service);

//rpc.StartListening();

await rpc.Completion;
