using System;

namespace VSCodeDebug
{
	public enum ProjectType
	{
		Mono,
		Android,
		iOS,

	}
	public class XamarinOptions
	{
		public ProjectType ProjectType { get; set; }
		public bool IsSim { get; set; }
		public string AppName { get; set; }
	}
}
