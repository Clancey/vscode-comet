using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Build.Execution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace Comet.Reload
{
    public class RoslynCodeManager
    {
        public static RoslynCodeManager Shared { get; set; } = new RoslynCodeManager();

        Dictionary<string, List<string>> referencesForProjects = new Dictionary<string, List<string>>();
        public bool ShouldHotReload(string project)
        {
            if (string.IsNullOrWhiteSpace(project))
                return false;
            var hasHotReload = GetReferences(project, null).Any(x => x.EndsWith("HotUI.dll"));
            return hasHotReload;
        }
        public void StartDebugging()
        {
        }
        public void StopDebugging()
        {
            referencesForProjects.Clear();
        }

        public List<string> GetReferences(string projectPath, string currentReference)
        {
            if (referencesForProjects.TryGetValue(projectPath, out var references))
                return references;
            var project = new ProjectInstance(projectPath);
			var items = project.ItemTypes.ToList();
            var result = BuildManager.DefaultBuildManager.Build(
                new BuildParameters(),
                new BuildRequestData(project, new[]
            {
                "ResolveProjectReferences",
                "ResolveAssemblyReferences"
            }));
            IEnumerable<string> GetResultItems(string targetName)
            {
                var buildResult = result.ResultsByTarget[targetName];
                var buildResultItems = buildResult.Items;

                return buildResultItems.Select(item => item.ItemSpec);
            }
            references = GetResultItems("ResolveProjectReferences")
                .Concat(GetResultItems("ResolveAssemblyReferences")).Distinct().ToList();
            if (!string.IsNullOrWhiteSpace(currentReference))
                references.Add(currentReference);
            referencesForProjects[projectPath] = references;
            return references;
        }

    }
}
