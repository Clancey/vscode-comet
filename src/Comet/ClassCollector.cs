using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Comet.Reload {
	public class ClassCollector : CSharpSyntaxWalker
	{
		public ICollection<UsingDirectiveSyntax> Usings { get; } = new List<UsingDirectiveSyntax> ();
		public ICollection<ClassDeclarationSyntax> Classes { get; } = new List<ClassDeclarationSyntax> ();

		public override void VisitUsingDirective (UsingDirectiveSyntax node)
		{
			base.VisitUsingDirective (node);
			Usings.Add (node);
		}
		public override void VisitClassDeclaration (ClassDeclarationSyntax node)
		{
			base.VisitClassDeclaration (node);
			if(!(node.Parent is ClassDeclarationSyntax))
				Classes.Add (node);
		}

	}

}
