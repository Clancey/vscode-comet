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

		public string OutputFolder(string configuration, string platform) => ProjectRoot().GetOutput(configuration, platform).ToNativePath();



		const string iOSGuid = "FEACFBD2-3405-455C-9665-78FE426C6842";
		const string androidGuid = "EFBA0AD7-5A72-4C68-AF49-83D382785DCF";
		const string uwpGuid = "A5A43C5B-DE2A-4C0C-9213-0A381AF9435A";
		const string wpfGuid = "60dc8134-eba5-43b8-bcc9-bb4bc16c2548";
		const string macOSGuid = "A3F8F2AB-B479-4A4A-A458-A89E7DC349F1";

		public ProjectType GetProjectType()
		{
			var projectTypeGuid = ProjectRoot().ProjectTypeGuids();
			if (projectTypeGuid.Contains(iOSGuid))
				return ProjectType.iOS;
			if (projectTypeGuid.Contains(androidGuid))
				return ProjectType.Android;
			if (projectTypeGuid.Contains(uwpGuid))
				return ProjectType.UWP;
			if (projectTypeGuid.Contains(wpfGuid))
				return ProjectType.WPF;
			//TODO: do some other types
			return ProjectType.Mono;
		}
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
