using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace VSCodeDebug
{
	public class ProjectParser
	{
		static XNamespace msbuild = "http://schemas.microsoft.com/developer/msbuild/2003";
		XDocument projDefinition;
		public ProjectParser(string csprojPath)
		{
			projDefinition = XDocument.Load(csprojPath);
		}

		XElement ProjectRoot() => projDefinition.Project();

		public string ProjectName() => ProjectRoot().ProjectName();

		public string OutputFolder(string configuration, string platform) => ProjectRoot().GetOutput(configuration, platform);

		//public string OutputPath(string configuration, string platform)
		//{
		//	var key = $"{configuration}|{platform}";
		//	projDefinition.Element
		//}
	}

	internal static class ProjectParserExtension
	{
		static XNamespace msbuild = "http://schemas.microsoft.com/developer/msbuild/2003";

		public static XElement Project(this XDocument document) => document.Element(msbuild + "Project");

		public static IEnumerable<XElement> ItemGroup(this XElement element) => element.Elements(msbuild + "ItemGroup");

		public static IEnumerable<XElement> PropertyGroup(this XElement element) => element.Elements(msbuild + "PropertyGroup");

		public static IEnumerable<XElement> GetConfiguration(this XElement project, string configuration, string platform) => project.GetConfiguration($"{configuration}|{platform}");
		public static IEnumerable<XElement> GetConfiguration(this XElement project, string conditionContains)
			=> project.PropertyGroup()
			.Where(x => x.Attribute("Condition")?.Value?.Contains(conditionContains) ?? false);

		public static string ProjectTypeGuids(this XElement project) => project.PropertyGroup().Elements(msbuild + "ProjectTypeGuids").FirstOrDefault()?.Value;

		public static string ProjectName(this XElement project) => project.PropertyGroup().Elements(msbuild + "AssemblyName").FirstOrDefault()?.Value;


		public static string GetOutput (this XElement project, string configuration, string platform)
			=> project.GetConfiguration(configuration,platform).Elements(msbuild + "OutputPath").FirstOrDefault()?.Value;


		public static IEnumerable<string> References(this XDocument document)
			=> document.Project().ItemGroup().Elements(msbuild + "ItemGroup")
			.Select(x => x.Attribute("Include")?.Value ?? x.Value);

	}
}
