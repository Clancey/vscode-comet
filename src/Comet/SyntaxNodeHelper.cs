using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Comet.Reload {
	public static class SyntaxNodeHelper {
		public static bool TryGetParentSyntax<T> (SyntaxNode syntaxNode, out T result)
		where T : SyntaxNode
		{
			// set defaults
			result = null;

			if (syntaxNode == null) {
				return false;
			}

			try {
				syntaxNode = syntaxNode.Parent;

				if (syntaxNode == null) {
					return false;
				}

				if (syntaxNode.GetType () == typeof (T)) {
					result = syntaxNode as T;
					return true;
				}

				return TryGetParentSyntax<T> (syntaxNode, out result);
			} catch {
				return false;
			}
		}

		public static (string NameSpace, string ClassName) GetClassNameWithNamespace (this ClassDeclarationSyntax c)
		{
			NamespaceDeclarationSyntax namespaceDeclaration;
			TryGetParentSyntax (c, out namespaceDeclaration);
			var theNameSpace = namespaceDeclaration?.Name?.ToString () ?? "";
			return (theNameSpace,c.Identifier.ToString() );
		}
	}
}
