using Microsoft.CodeAnalysis;

namespace Comet.Reload {
	/// <summary>
	/// Document changed event arguments.
	/// </summary>
	public class DocumentChangedEventArgs
	{
		public DocumentChangedEventArgs(string filename, string text)
		{
			Filename = filename;
			Text = text;
		}

		/// <summary>
		/// Gets the filename changed.
		/// </summary>
		/// <value>The filename.</value>
		public string Filename { get; }

		/// <summary>
		/// Gets the text contents of the file changed.
		/// </summary>
		/// <value>The text.</value>
		public string Text { get; set; }
	}
}