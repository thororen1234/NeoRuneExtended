using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NeoRuneExtended.Discovery;

namespace NeoRuneExtended.Bindings;

public sealed class BindingsGenerator
{
	private readonly ApiDump dump;

	private readonly Dictionary<string, ApiType> byPath;

	private readonly Dictionary<string, string> csNames = new Dictionary<string, string>();

	private readonly HashSet<string> actorClasses = new HashSet<string>();

	private const string ActorPath = "/Script/Engine.Actor";

	private static readonly HashSet<string> Keywords = new HashSet<string>
	{
		"abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
		"class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum",
		"event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto",
		"if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
		"new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public",
		"readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
		"struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
		"unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
	};

	public List<string> Warnings { get; } = new List<string>();

	public BindingsGenerator(ApiDump dump)
	{
		this.dump = dump;
		byPath = (from t in dump.Types
			group t by t.Path).ToDictionary((IGrouping<string, ApiType> g) => g.Key, (IGrouping<string, ApiType> g) => g.First());
		foreach (ApiType item in dump.Types.Where((ApiType t) => t.Kind == "class"))
		{
			if (IsActor(item))
			{
				actorClasses.Add(item.Path);
			}
		}
		foreach (ApiType type in dump.Types)
		{
			csNames[type.Path] = "global::" + Namespace(type.Path) + "." + TypeName(type);
		}
		foreach (ApiType item2 in dump.Types.Where((ApiType t) => t.Kind == "class"))
		{
			foreach (ApiFunction item3 in (item2.Functions ?? new List<ApiFunction>()).Where((ApiFunction f) => (f.Flags & 0x100000) != 0))
			{
				csNames[item2.Path + ":" + item3.Name] = csNames[item2.Path] + "." + NestedDelegateName(item2, item3);
			}
		}
	}

	private string NestedDelegateName(ApiType owner, ApiFunction fn)
	{
		string text = Sanitize(fn.Name.Replace("__DelegateSignature", ""));
		if (!(text == TypeName(owner)))
		{
			return text;
		}
		return text + "_Delegate";
	}

	private bool IsActor(ApiType t)
	{
		ApiType apiType = t;
		while (apiType != null)
		{
			if (apiType.Path == "/Script/Engine.Actor")
			{
				return true;
			}
			apiType = ((apiType.Super != null && byPath.TryGetValue(apiType.Super, out ApiType value)) ? value : null);
		}
		return false;
	}

	public static string Namespace(string path)
	{
		IEnumerable<string> values = (from p in path.Split('.')[0].Split('/', StringSplitOptions.RemoveEmptyEntries)
			where p != "Script"
			select p).Select(Sanitize);
		return "UE." + string.Join(".", values);
	}

	private static string Sanitize(string s)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (char c in s)
		{
			stringBuilder.Append((char.IsLetterOrDigit(c) || c == '_') ? c : '_');
		}
		if (stringBuilder.Length == 0 || char.IsDigit(stringBuilder[0]))
		{
			stringBuilder.Insert(0, '_');
		}
		return stringBuilder.ToString();
	}

	private static string Ident(string s)
	{
		string text = Sanitize(s);
		if (!Keywords.Contains(text))
		{
			return text;
		}
		return "@" + text;
	}

	private string TypeName(ApiType t)
	{
		string text = t.Path.Split('.', ':')[^1];
		return t.Kind switch
		{
			"class" => (actorClasses.Contains(t.Path) ? "A" : "U") + Sanitize(text), 
			"struct" => "F" + Sanitize(text), 
			"delegate" => Sanitize(text.Replace("__DelegateSignature", "")), 
			_ => Sanitize(text), 
		};
	}

	private string? CsType(TypeRef t)
	{
		object result;
		switch (t.Kind)
		{
		case "bool":
			return "bool";
		case "int8":
			return "sbyte";
		case "byte":
			return "byte";
		case "int16":
			return "short";
		case "int":
			return "int";
		case "int64":
			return "long";
		case "uint16":
			return "ushort";
		case "uint32":
			return "uint";
		case "uint64":
			return "ulong";
		case "float":
			return "float";
		case "double":
			return "double";
		case "string":
			return "string";
		case "name":
			return "global::NeoRune.FName";
		case "text":
			return "global::NeoRune.FText";
		case "object":
		case "interface":
		{
			string text6 = named(t.Path);
			return (text6 != null) ? (text6 + "?") : null;
		}
		case "class":
		{
			string text3 = named(t.Meta);
			return (text3 != null) ? ("global::NeoRune.TSubclassOf<" + text3 + ">") : null;
		}
		case "softobject":
		{
			string text9 = named(t.Path);
			return (text9 != null) ? ("global::NeoRune.TSoftObjectPtr<" + text9 + ">") : null;
		}
		case "softclass":
		{
			string text8 = named(t.Meta);
			return (text8 != null) ? ("global::NeoRune.TSoftClassPtr<" + text8 + ">") : null;
		}
		case "weakobject":
		{
			string text7 = named(t.Path);
			return (text7 != null) ? ("global::NeoRune.TWeakObjectPtr<" + text7 + ">") : null;
		}
		case "struct":
			return named(t.Path);
		case "enum":
			return named(t.Path);
		case "delegate":
		case "multicastdelegate":
			return named(t.Path);
		case "array":
		{
			string text5 = CsType(t.Inner);
			return (text5 != null) ? ("global::System.Collections.Generic.List<" + text5.TrimEnd('?') + ">") : null;
		}
		case "set":
		{
			string text4 = CsType(t.Inner);
			return (text4 != null) ? ("global::System.Collections.Generic.HashSet<" + text4.TrimEnd('?') + ">") : null;
		}
		case "map":
		{
			string text = CsType(t.Inner);
			if (text != null)
			{
				string text2 = CsType(t.Value);
				if (text2 != null)
				{
					result = $"global::System.Collections.Generic.Dictionary<{text.TrimEnd('?')}, {text2.TrimEnd('?')}>";
					goto IL_0689;
				}
			}
			result = null;
			goto IL_0689;
		}
		default:
			{
				return null;
			}
			IL_0689:
			return (string?)result;
		}
		string? named(string? path)
		{
			if (path == null || !csNames.TryGetValue(path, out string value))
			{
				return null;
			}
			return value;
		}
	}

	private string? Descriptor(TypeRef t)
	{
		switch (t.Kind)
		{
		case "int8":
		case "name":
		case "text":
		case "bool":
		case "byte":
		case "float":
		case "int64":
		case "int16":
		case "int":
		case "double":
		case "string":
		case "uint16":
		case "uint32":
		case "uint64":
			return t.Kind;
		case "object":
			if (!known(t.Path))
			{
				return null;
			}
			return "object(" + t.Path + ")";
		case "interface":
			if (!known(t.Path))
			{
				return null;
			}
			return "interface(" + t.Path + ")";
		case "class":
			if (!known(t.Meta))
			{
				return null;
			}
			return "class(" + t.Meta + ")";
		case "softobject":
			if (!known(t.Path))
			{
				return null;
			}
			return "softobject(" + t.Path + ")";
		case "softclass":
			if (!known(t.Meta))
			{
				return null;
			}
			return "softclass(" + t.Meta + ")";
		case "weakobject":
			if (!known(t.Path))
			{
				return null;
			}
			return "weakobject(" + t.Path + ")";
		case "struct":
		{
			if (!known(t.Path))
			{
				return null;
			}
			ApiType value;
			return $"struct({t.Path},{(byPath.TryGetValue(t.Path, out value) ? value.Size : 0)})";
		}
		case "enum":
		{
			if (!known(t.Path))
			{
				return null;
			}
			string text3 = t.Inner?.Kind;
			bool flag = ((text3 == null || text3 == "byte") ? true : false);
			return flag ? ("enum(" + t.Path + ")") : $"enum({t.Path},{t.Inner.Kind})";
		}
		case "delegate":
			if (!known(t.Path))
			{
				return null;
			}
			return "delegate(" + t.Path + ")";
		case "multicastdelegate":
			if (!known(t.Path))
			{
				return null;
			}
			return "mdelegate(" + t.Path + ")";
		case "array":
		{
			string text5 = Descriptor(t.Inner);
			if (text5 == null)
			{
				return null;
			}
			return "array(" + text5 + ")";
		}
		case "set":
		{
			string text4 = Descriptor(t.Inner);
			if (text4 == null)
			{
				return null;
			}
			return "set(" + text4 + ")";
		}
		case "map":
		{
			string text = Descriptor(t.Inner);
			if (text != null)
			{
				string text2 = Descriptor(t.Value);
				if (text2 != null)
				{
					return $"map({text},{text2})";
				}
			}
			return null;
		}
		default:
			return null;
		}
		static bool known(string? p)
		{
			if (p != null)
			{
				return p != "?";
			}
			return false;
		}
	}

	public Dictionary<string, string> Generate()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (IGrouping<string, ApiType> item in from t in dump.Types
			group t by Namespace(t.Path) into g
			orderby g.Key
			select g)
		{
			StringBuilder stringBuilder = new StringBuilder();
			string buildId = dump.Game.BuildId;
			stringBuilder.AppendLine("// <auto-generated> NeoRune bindings for " + ((buildId != null && buildId.Length > 0) ? ("build " + buildId) : dump.Game.ExeStamp) + " </auto-generated>");
			stringBuilder.AppendLine("#nullable enable");
			stringBuilder.AppendLine("#pragma warning disable CS0108, CS0114, CS0067, CS8618, CS1591");
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
			handler.AppendLiteral("namespace ");
			handler.AppendFormatted(item.Key);
			stringBuilder2.AppendLine(ref handler);
			stringBuilder.AppendLine("{");
			foreach (ApiType item2 in item)
			{
				switch (item2.Kind)
				{
				case "class":
					EmitClass(stringBuilder, item2);
					break;
				case "struct":
					EmitStruct(stringBuilder, item2);
					break;
				case "enum":
					EmitEnum(stringBuilder, item2);
					break;
				case "delegate":
					EmitDelegate(stringBuilder, item2.Functions[0], item2.Path, TypeName(item2), "    ");
					break;
				}
			}
			stringBuilder.AppendLine("}");
			dictionary[item.Key + ".g.cs"] = stringBuilder.ToString();
		}
		return dictionary;
	}

	private void EmitEnum(StringBuilder sb, ApiType t)
	{
		string value = TypeName(t);
		string value2 = "long";
		StringBuilder stringBuilder = sb;
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(31, 1, stringBuilder);
		handler.AppendLiteral("    [global::NeoRune.UEnum(\"");
		handler.AppendFormatted(t.Path);
		handler.AppendLiteral("\")]");
		stringBuilder2.AppendLine(ref handler);
		stringBuilder = sb;
		StringBuilder stringBuilder3 = stringBuilder;
		handler = new StringBuilder.AppendInterpolatedStringHandler(19, 2, stringBuilder);
		handler.AppendLiteral("    public enum ");
		handler.AppendFormatted(value);
		handler.AppendLiteral(" : ");
		handler.AppendFormatted(value2);
		stringBuilder3.AppendLine(ref handler);
		sb.AppendLine("    {");
		HashSet<string> hashSet = new HashSet<string>();
		foreach (ApiEnumValue item in t.Values ?? new List<ApiEnumValue>())
		{
			string s;
			if (!item.Name.Contains("::"))
			{
				s = item.Name;
			}
			else
			{
				string name = item.Name;
				int num = item.Name.LastIndexOf("::") + 2;
				s = name.Substring(num, name.Length - num);
			}
			string text = Ident(s);
			if (hashSet.Add(text))
			{
				stringBuilder = sb;
				StringBuilder stringBuilder4 = stringBuilder;
				handler = new StringBuilder.AppendInterpolatedStringHandler(12, 2, stringBuilder);
				handler.AppendLiteral("        ");
				handler.AppendFormatted(text);
				handler.AppendLiteral(" = ");
				handler.AppendFormatted(item.Value);
				handler.AppendLiteral(",");
				stringBuilder4.AppendLine(ref handler);
			}
		}
		sb.AppendLine("    }");
	}

	private void EmitStruct(StringBuilder sb, ApiType t)
	{
		string text = TypeName(t);
		StringBuilder stringBuilder = sb;
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(35, 2, stringBuilder);
		handler.AppendLiteral("    [global::NeoRune.UStruct(\"");
		handler.AppendFormatted(t.Path);
		handler.AppendLiteral("\", ");
		handler.AppendFormatted(t.Size);
		handler.AppendLiteral(")]");
		stringBuilder2.AppendLine(ref handler);
		stringBuilder = sb;
		StringBuilder stringBuilder3 = stringBuilder;
		handler = new StringBuilder.AppendInterpolatedStringHandler(26, 1, stringBuilder);
		handler.AppendLiteral("    public partial struct ");
		handler.AppendFormatted(text);
		stringBuilder3.AppendLine(ref handler);
		sb.AppendLine("    {");
		HashSet<string> used = new HashSet<string> { text };
		foreach (ApiType item in StructChain(t))
		{
			foreach (ApiProperty item2 in item.Properties ?? new List<ApiProperty>())
			{
				string text2 = CsType(item2.Type);
				string text3 = Descriptor(item2.Type);
				if (text2 != null && text3 != null)
				{
					string value = Unique(Ident(item2.Name), used);
					string value2 = ((item == t) ? "" : (", \"" + item.Path + "\""));
					stringBuilder = sb;
					StringBuilder stringBuilder4 = stringBuilder;
					handler = new StringBuilder.AppendInterpolatedStringHandler(53, 5, stringBuilder);
					handler.AppendLiteral("        [global::NeoRune.UProperty(\"");
					handler.AppendFormatted(item2.Name);
					handler.AppendLiteral("\", \"");
					handler.AppendFormatted(text3);
					handler.AppendLiteral("\"");
					handler.AppendFormatted(value2);
					handler.AppendLiteral(")] public ");
					handler.AppendFormatted(text2);
					handler.AppendLiteral(" ");
					handler.AppendFormatted(value);
					handler.AppendLiteral(";");
					stringBuilder4.AppendLine(ref handler);
				}
			}
		}
		sb.AppendLine("    }");
	}

	private List<ApiType> StructChain(ApiType t)
	{
		List<ApiType> list = new List<ApiType>();
		ApiType apiType = t;
		while (apiType != null && list.Count < 32)
		{
			list.Insert(0, apiType);
			apiType = ((apiType.Super != null && byPath.TryGetValue(apiType.Super, out ApiType value) && value.Kind == "struct") ? value : null);
		}
		return list;
	}

	private static string Unique(string id, HashSet<string> used)
	{
		string text = id;
		int num = 2;
		while (!used.Add(text))
		{
			text = id + "_" + num;
			num++;
		}
		return text;
	}

	private void EmitClass(StringBuilder sb, ApiType t)
	{
		string text = TypeName(t);
		string text2 = ((t.Super != null && csNames.TryGetValue(t.Super, out string value)) ? value : null);
		bool scriptOnlyClass = t.MetaClass == "ASClass";
		StringBuilder stringBuilder = sb;
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(32, 1, stringBuilder);
		handler.AppendLiteral("    [global::NeoRune.UClass(\"");
		handler.AppendFormatted(t.Path);
		handler.AppendLiteral("\")]");
		stringBuilder2.AppendLine(ref handler);
		stringBuilder = sb;
		StringBuilder stringBuilder3 = stringBuilder;
		handler = new StringBuilder.AppendInterpolatedStringHandler(25, 2, stringBuilder);
		handler.AppendLiteral("    public partial class ");
		handler.AppendFormatted(text);
		handler.AppendFormatted((text2 != null) ? (" : " + text2) : "");
		stringBuilder3.AppendLine(ref handler);
		sb.AppendLine("    {");
		HashSet<string> hashSet = new HashSet<string> { text };
		foreach (ApiFunction item in (t.Functions ?? new List<ApiFunction>()).Where((ApiFunction f) => (f.Flags & 0x100000) != 0))
		{
			string text3 = NestedDelegateName(t, item);
			hashSet.Add(text3);
			EmitDelegate(sb, item, t.Path + ":" + item.Name, text3, "        ");
		}
		foreach (ApiProperty item2 in t.Properties ?? new List<ApiProperty>())
		{
			string text4 = Descriptor(item2.Type);
			string text5 = CsType(item2.Type);
			if (text4 != null && text5 != null)
			{
				string value2 = Unique(Ident(item2.Name), hashSet);
				if (item2.Type.Kind == "multicastdelegate")
				{
					stringBuilder = sb;
					StringBuilder stringBuilder4 = stringBuilder;
					handler = new StringBuilder.AppendInterpolatedStringHandler(81, 4, stringBuilder);
					handler.AppendLiteral("        [global::NeoRune.UProperty(\"");
					handler.AppendFormatted(item2.Name);
					handler.AppendLiteral("\", \"");
					handler.AppendFormatted(text4);
					handler.AppendLiteral("\")] public event ");
					handler.AppendFormatted(text5);
					handler.AppendLiteral(" ");
					handler.AppendFormatted(value2);
					handler.AppendLiteral(" { add { } remove { } }");
					stringBuilder4.AppendLine(ref handler);
				}
				else
				{
					stringBuilder = sb;
					StringBuilder stringBuilder5 = stringBuilder;
					handler = new StringBuilder.AppendInterpolatedStringHandler(96, 4, stringBuilder);
					handler.AppendLiteral("        [global::NeoRune.UProperty(\"");
					handler.AppendFormatted(item2.Name);
					handler.AppendLiteral("\", \"");
					handler.AppendFormatted(text4);
					handler.AppendLiteral("\")] public ");
					handler.AppendFormatted(text5);
					handler.AppendLiteral(" ");
					handler.AppendFormatted(value2);
					handler.AppendLiteral(" { get => throw null!; set => throw null!; }");
					stringBuilder5.AppendLine(ref handler);
				}
			}
		}
		foreach (ApiFunction item3 in t.Functions ?? new List<ApiFunction>())
		{
			if ((item3.Flags & 0x100000) == 0)
			{
				EmitFunction(sb, t, item3, hashSet, scriptOnlyClass);
			}
		}
		sb.AppendLine("    }");
	}

	private void EmitFunction(StringBuilder sb, ApiType owner, ApiFunction fn, HashSet<string> used, bool scriptOnlyClass)
	{
		string text = null;
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		foreach (ApiProperty item in fn.Params)
		{
			string text2 = Descriptor(item.Type);
			string text3 = CsType(item.Type);
			if (text2 == null || text3 == null)
			{
				Warnings.Add($"skipped {owner.Path}:{fn.Name} (param {item.Name}: {item.Type})");
				return;
			}
			list2.Add($"{item.Name}:{text2}:{item.Flags:x}");
			if ((item.Flags & 0x400) != 0L)
			{
				text = text3;
				continue;
			}
			bool flag = (item.Flags & 0x100) != 0L && (item.Flags & 2) == 0;
			bool flag2 = flag && (item.Flags & 0x8000000) != 0;
			list.Add((flag2 ? "ref " : (flag ? "out " : "")) + text3 + " " + Ident(item.Name));
		}
		string value = Unique(Ident(fn.Name), used);
		bool flag3 = (fn.Flags & 0x2000) != 0;
		bool flag4 = (fn.Flags & 0x8000000) != 0 && !flag3;
		bool flag5 = (fn.Flags & 0x400) != 0;
		string value2 = ((((fn.Flags & 0x80000) != 0) & flag4) ? "protected" : "public");
		string value3 = (flag3 ? " static" : (flag4 ? " virtual" : ""));
		string text4 = $"[global::NeoRune.UFunction(\"{fn.Name}\", 0x{fn.Flags:x}, \"{string.Join(";", list2)}\")]";
		if ((!flag5 && !flag4) || (scriptOnlyClass && !flag5 && !flag4))
		{
			text4 += " [global::NeoRune.ScriptOnly]";
		}
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(29, 6, sb);
		handler.AppendLiteral("        ");
		handler.AppendFormatted(text4);
		handler.AppendLiteral(" ");
		handler.AppendFormatted(value2);
		handler.AppendFormatted(value3);
		handler.AppendLiteral(" ");
		handler.AppendFormatted(text ?? "void");
		handler.AppendLiteral(" ");
		handler.AppendFormatted(value);
		handler.AppendLiteral("(");
		handler.AppendFormatted(string.Join(", ", list));
		handler.AppendLiteral(") => throw null!;");
		sb.AppendLine(ref handler);
	}

	private void EmitDelegate(StringBuilder sb, ApiFunction fn, string path, string name, string indent)
	{
		string text = null;
		List<string> list = new List<string>();
		foreach (ApiProperty item in fn.Params)
		{
			string text2 = CsType(item.Type);
			if (text2 == null)
			{
				Warnings.Add("skipped delegate " + path);
				return;
			}
			if ((item.Flags & 0x400) != 0L)
			{
				text = text2;
				continue;
			}
			bool flag = (item.Flags & 0x100) != 0L && (item.Flags & 2) == 0;
			list.Add((flag ? "ref " : "") + text2 + " " + Ident(item.Name));
		}
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(52, 5, sb);
		handler.AppendFormatted(indent);
		handler.AppendLiteral("[global::NeoRune.UDelegate(\"");
		handler.AppendFormatted(path);
		handler.AppendLiteral("\")] public delegate ");
		handler.AppendFormatted(text ?? "void");
		handler.AppendLiteral(" ");
		handler.AppendFormatted(name);
		handler.AppendLiteral("(");
		handler.AppendFormatted(string.Join(", ", list));
		handler.AppendLiteral(");");
		sb.AppendLine(ref handler);
	}
}
