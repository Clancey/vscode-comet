using System.Diagnostics.Tracing;
using DotNetWorkspaceAnalyzer;
using StreamJsonRpc;


WorkspaceAnalyzer? service;

Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults();

service = new WorkspaceAnalyzer();

//service.WorkspaceChanged += Service_WorkspaceChanged;
//void Service_WorkspaceChanged(object? sender, WorkspaceInfo e)
//{
//	foreach (var p in e.Solution.Projects)
//	{
//		Console.WriteLine(p.Name);
//	}
//}
//await service.OpenWorkspace(@"C:\Users\jondi\OneDrive\Desktop\MauiRc2\MauiRc2.sln", "Debug", "AnyCPU");

//await service.OpenWorkspace("/Users/redth/Desktop/MauiTest/MauiTest.sln", "Debug", "AnyCPU");


var rpc = JsonRpc.Attach(Console.OpenStandardOutput(), Console.OpenStandardInput(), service);

//rpc.StartListening();

await rpc.Completion;
