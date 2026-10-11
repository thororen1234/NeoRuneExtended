using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NeoRuneExtended.Cli;

internal static class ApiDocsCommand
{
	public static int Run(Args a)
	{
		string? path = a.Get("sdk") ?? Path.Combine("sdk", "NeoRuneExtended.Sdk", "src");
		string path2 = a.Get("abstractions") ?? Path.Combine("src", "NeoRuneExtended.Abstractions");
		string text = a.Get("out") ?? Path.Combine("docs", "api-reference.md");
		IOrderedEnumerable<string> files = Directory.EnumerateFiles(path, "*.cs").Concat(Directory.EnumerateFiles(path2, "*.cs")).OrderBy<string, string>((string f) => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase);
		File.WriteAllText(text, Generate(files));
		Console.WriteLine("Wrote " + text);
		return 0;
	}

	internal static string Generate(IEnumerable<string> files)
	{
		List<(string, string, string, List<(string, string)>)> list = new List<(string, string, string, List<(string, string)>)>();
		foreach (string file in files)
		{
			SyntaxNode root = CSharpSyntaxTree.ParseText(File.ReadAllText(file), new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse, SourceCodeKind.Regular, new string[3] { "NEORUNE_PROFILE", "NEORUNE_TRANSLATIONS", "NEORUNE_TUNING" })).GetRoot();
			foreach (BaseTypeDeclarationSyntax item7 in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
			{
				if (!IsPublic(item7.Modifiers) || item7.Parent is BaseTypeDeclarationSyntax || !InNeoRune(item7))
				{
					continue;
				}
				List<(string, string)> list2 = new List<(string, string)>();
				if (item7 is TypeDeclarationSyntax typeDeclarationSyntax)
				{
					foreach (MemberDeclarationSyntax member in typeDeclarationSyntax.Members)
					{
						if (IsPublic(member.Modifiers) && !(member is BaseTypeDeclarationSyntax))
						{
							list2.Add((Signature(member), Summary(member)));
						}
					}
				}
				BaseTypeDeclarationSyntax baseTypeDeclarationSyntax = item7;
				string text = ((baseTypeDeclarationSyntax is InterfaceDeclarationSyntax) ? "interface" : ((baseTypeDeclarationSyntax is StructDeclarationSyntax) ? "struct" : ((!(baseTypeDeclarationSyntax is EnumDeclarationSyntax)) ? ((!item7.Modifiers.Any(SyntaxKind.StaticKeyword)) ? "class" : "static class") : "enum")));
				string item = text;
				list.Add((item7.Identifier.Text, item, Summary(item7), list2));
			}
			foreach (DelegateDeclarationSyntax item8 in from d in root.DescendantNodes().OfType<DelegateDeclarationSyntax>()
				where IsPublic(d.Modifiers) && InNeoRune(d)
				select d)
			{
				list.Add((item8.Identifier.Text, "delegate", Summary(item8), new List<(string, string)> { (Clean(item8.WithAttributeLists(default(SyntaxList<AttributeListSyntax>)).WithoutTrivia().ToString()), "") }));
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("# API reference");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("Every public type and member of the `NeoRune` namespace, generated from the SDK's doc comments (`neorunex apidocs`).");
		stringBuilder.AppendLine("The [SDK reference](sdk-reference.md) explains them by topic, with examples.");
		stringBuilder.AppendLine();
		foreach (IGrouping<string, (string, string, string, List<(string, string)>)> item9 in (from t in list
			group t by t.Item1).OrderBy<IGrouping<string, (string, string, string, List<(string, string)>)>, string>((IGrouping<string, (string Name, string Kind, string Summary, List<(string Signature, string Summary)> Members)> g) => g.Key, StringComparer.OrdinalIgnoreCase))
		{
			(string, string, string, List<(string, string)>) tuple = item9.First();
			string item2 = tuple.Item1;
			string item3 = tuple.Item2;
			string item4 = tuple.Item3;
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder3 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(3, 1, stringBuilder2);
			handler.AppendLiteral("## ");
			handler.AppendFormatted(item2);
			stringBuilder3.AppendLine(ref handler);
			stringBuilder.AppendLine();
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder4 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(2, 2, stringBuilder2);
			handler.AppendLiteral("*");
			handler.AppendFormatted(item3);
			handler.AppendLiteral("*");
			handler.AppendFormatted((item4.Length > 0) ? (": " + item4) : "");
			stringBuilder4.AppendLine(ref handler);
			stringBuilder.AppendLine();
			List<(string, string)> list3 = item9.SelectMany<(string, string, string, List<(string, string)>), (string, string)>(((string Name, string Kind, string Summary, List<(string Signature, string Summary)> Members) g) => g.Members).ToList();
			if (list3.Count == 0)
			{
				continue;
			}
			stringBuilder.AppendLine("| Member | |");
			stringBuilder.AppendLine("|---|---|");
			foreach (var item10 in list3)
			{
				string item5 = item10.Item1;
				string item6 = item10.Item2;
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder5 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(9, 2, stringBuilder2);
				handler.AppendLiteral("| `");
				handler.AppendFormatted(item5.Replace("|", "\\|"));
				handler.AppendLiteral("` | ");
				handler.AppendFormatted(item6.Replace("|", "\\|"));
				handler.AppendLiteral(" |");
				stringBuilder5.AppendLine(ref handler);
			}
			stringBuilder.AppendLine();
		}
		return stringBuilder.ToString();
	}

	private static bool IsPublic(SyntaxTokenList modifiers)
	{
		return modifiers.Any(SyntaxKind.PublicKeyword);
	}

	private static bool InNeoRune(SyntaxNode node)
	{
		return node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString() == "NeoRune";
	}

	private static string Signature(MemberDeclarationSyntax m)
	{
		MemberDeclarationSyntax memberDeclarationSyntax = m.WithAttributeLists(default(SyntaxList<AttributeListSyntax>)).WithoutTrivia();
		string s = ((memberDeclarationSyntax is MethodDeclarationSyntax methodDeclarationSyntax) ? methodDeclarationSyntax.WithBody(null).WithExpressionBody(null).WithSemicolonToken(default(SyntaxToken))
			.ToString() : ((memberDeclarationSyntax is ConstructorDeclarationSyntax constructorDeclarationSyntax) ? constructorDeclarationSyntax.WithBody(null).WithExpressionBody(null).WithSemicolonToken(default(SyntaxToken))
			.WithInitializer(null)
			.ToString() : ((memberDeclarationSyntax is PropertyDeclarationSyntax propertyDeclarationSyntax) ? (propertyDeclarationSyntax.WithExpressionBody(null).WithInitializer(null).WithSemicolonToken(default(SyntaxToken))
			.WithAccessorList((propertyDeclarationSyntax.AccessorList == null) ? null : SyntaxFactory.AccessorList(SyntaxFactory.List(propertyDeclarationSyntax.AccessorList.Accessors.Select((AccessorDeclarationSyntax acc) => acc.WithBody(null).WithExpressionBody(null).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))))))
			.ToString() + ((propertyDeclarationSyntax.ExpressionBody != null) ? " { get; }" : "")) : ((memberDeclarationSyntax is FieldDeclarationSyntax fieldDeclarationSyntax) ? fieldDeclarationSyntax.WithDeclaration(fieldDeclarationSyntax.Declaration.WithVariables(SyntaxFactory.SeparatedList(fieldDeclarationSyntax.Declaration.Variables.Select((VariableDeclaratorSyntax v) => v.WithInitializer(null))))).ToString() : ((memberDeclarationSyntax is EventFieldDeclarationSyntax eventFieldDeclarationSyntax) ? eventFieldDeclarationSyntax.ToString() : ((memberDeclarationSyntax is OperatorDeclarationSyntax operatorDeclarationSyntax) ? operatorDeclarationSyntax.WithBody(null).WithExpressionBody(null).WithSemicolonToken(default(SyntaxToken))
			.ToString() : ((!(memberDeclarationSyntax is ConversionOperatorDeclarationSyntax conversionOperatorDeclarationSyntax)) ? memberDeclarationSyntax.ToString() : conversionOperatorDeclarationSyntax.WithBody(null).WithExpressionBody(null).WithSemicolonToken(default(SyntaxToken))
			.ToString())))))));
		return Clean(s);
	}

	private static string Clean(string s)
	{
		return Regex.Replace(s, "\\s+", " ").Trim().TrimEnd(';')
			.Replace("public ", "");
	}

	private static string Summary(SyntaxNode node)
	{
		DocumentationCommentTriviaSyntax documentationCommentTriviaSyntax = (from t in node.GetLeadingTrivia()
			select t.GetStructure()).OfType<DocumentationCommentTriviaSyntax>().FirstOrDefault();
		if (documentationCommentTriviaSyntax == null)
		{
			return "";
		}
		string text = string.Join("\n", from l in documentationCommentTriviaSyntax.ToFullString().Split('\n')
			select Regex.Replace(l, "^\\s*///\\s?", ""));
		try
		{
			XElement xElement = XElement.Parse("<doc>" + text + "</doc>").Element("summary");
			if (xElement == null)
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder();
			foreach (XNode item in xElement.Nodes())
			{
				if (item is XText xText)
				{
					stringBuilder.Append(xText.Value);
				}
				else if (item is XElement xElement2)
				{
					StringBuilder stringBuilder2 = stringBuilder;
					string value;
					switch (xElement2.Name.LocalName)
					{
					case "see":
						value = "`" + Short(xElement2.Attribute("cref")?.Value ?? xElement2.Attribute("langword")?.Value ?? "") + "`";
						break;
					case "paramref":
					case "typeparamref":
						value = "`" + xElement2.Attribute("name")?.Value + "`";
						break;
					case "c":
						value = "`" + xElement2.Value + "`";
						break;
					case "code":
						value = " (example in the source) ";
						break;
					case "para":
						value = " " + xElement2.Value;
						break;
					default:
						value = xElement2.Value;
						break;
					}
					stringBuilder2.Append(value);
				}
			}
			return Regex.Replace(stringBuilder.ToString(), "\\s+", " ").Trim();
		}
		catch (XmlException)
		{
			return "";
		}
	}

	private static string Short(string cref)
	{
		return Regex.Replace(cref, "\\(.*\\)", "").Split('.').Last();
	}
}
