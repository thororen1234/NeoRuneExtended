using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using NeoRuneExtended.Assets;

namespace NeoRuneExtended.Compiler;

public sealed class Symbols
{
	private readonly Compilation compilation;

	public string ModRoot { get; }

	public INamedTypeSymbol? List { get; }

	public INamedTypeSymbol? HashSet { get; }

	public INamedTypeSymbol? Dictionary { get; }

	public event Action<INamedTypeSymbol>? SourceClassUsed;

	public Symbols(Compilation compilation, string modRoot)
	{
		this.compilation = compilation;
		ModRoot = modRoot;
		List = compilation.GetTypeByMetadataName("System.Collections.Generic.List`1");
		HashSet = compilation.GetTypeByMetadataName("System.Collections.Generic.HashSet`1");
		Dictionary = compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2");
	}

	private static AttributeData? Attr(ISymbol s, string name)
	{
		return s.GetAttributes().FirstOrDefault((AttributeData a) => a.AttributeClass?.Name == name && a.AttributeClass.ContainingNamespace?.Name == "NeoRune");
	}

	public static string? ClassPathAttr(ITypeSymbol t)
	{
		return Attr(t, "UClassAttribute")?.ConstructorArguments[0].Value as string;
	}

	/// <summary>The package path of a mod type with [Asset], or null.</summary>
	public static string? AssetPackage(ITypeSymbol t)
	{
		return Attr(t, "AssetAttribute")?.ConstructorArguments[0].Value as string;
	}

	/// <summary>The GUID [Asset] gives a struct, or null.</summary>
	public static string? AssetGuid(ITypeSymbol t)
	{
		return Attr(t, "AssetAttribute")?.NamedArguments.FirstOrDefault((KeyValuePair<string, TypedConstant> a) => a.Key == "Guid").Value.Value as string;
	}

	/// <summary>A mod type's package: its [Asset] path, or the mod's folder.</summary>
	public string ModPackage(ITypeSymbol t)
	{
		return AssetPackage(t) ?? (ModRoot + "/" + t.Name);
	}

	/// <summary>The asset name: the package path's last part.</summary>
	public static string AssetName(string package)
	{
		return package.Substring(package.LastIndexOf('/') + 1);
	}

	/// <summary>A member's Unreal name: its [UName], or its own.</summary>
	public static string UnrealName(ISymbol s)
	{
		return Attr(s, "UNameAttribute")?.ConstructorArguments[0].Value as string ?? s.Name;
	}

	public static string? IntrinsicId(ISymbol s)
	{
		return Attr(s, "IntrinsicAttribute")?.ConstructorArguments[0].Value as string;
	}

	public static bool IsScriptOnly(ISymbol s)
	{
		return Attr(s, "ScriptOnlyAttribute") != null;
	}

	public static bool IsSource(ITypeSymbol t)
	{
		return t.Locations.Any((Location l) => l.IsInSource);
	}

	public string? ClassPath(ITypeSymbol t, bool use = true)
	{
		string text = ClassPathAttr(t);
		if (text != null)
		{
			return text;
		}
		if (t is INamedTypeSymbol namedTypeSymbol && IsSource(namedTypeSymbol) && namedTypeSymbol.TypeKind == TypeKind.Class)
		{
			if (namedTypeSymbol.IsAbstract)
			{
				INamedTypeSymbol baseType = namedTypeSymbol.BaseType;
				if (baseType == null)
				{
					return null;
				}
				return ClassPath(baseType, use);
			}
			if (use)
			{
				SourceClassUsed?.Invoke(namedTypeSymbol);
			}
			string package = ModPackage(namedTypeSymbol);
			return $"{package}.{AssetName(package)}_C";
		}
		// A Blueprint interface the mod generates ([Asset]).
		if (t is INamedTypeSymbol { TypeKind: TypeKind.Interface } interfaceSymbol && IsSource(interfaceSymbol))
		{
			string package2 = AssetPackage(interfaceSymbol);
			if (package2 != null)
			{
				return $"{package2}.{AssetName(package2)}_C";
			}
		}
		return null;
	}

	public static bool IsModStruct(ITypeSymbol t)
	{
		if (t.TypeKind == TypeKind.Struct)
		{
			return IsSource(t);
		}
		return false;
	}

	public string ModStructPath(ITypeSymbol t)
	{
		string package = ModPackage(t);
		return $"{package}.{AssetName(package)}";
	}

	public static IEnumerable<ISymbol> ModStructFields(INamedTypeSymbol t)
	{
		return t.GetMembers().Where(delegate(ISymbol m)
		{
			bool flag = !m.IsStatic;
			if (flag)
			{
				bool flag2;
				if (!(m is IFieldSymbol fieldSymbol))
				{
					IPropertySymbol propertySymbol = m as IPropertySymbol;
					flag2 = propertySymbol != null && t.GetMembers().OfType<IFieldSymbol>().Any((IFieldSymbol f) => SymbolEqualityComparer.Default.Equals(f.AssociatedSymbol, propertySymbol));
				}
				else
				{
					flag2 = !fieldSymbol.IsConst && fieldSymbol.AssociatedSymbol == null;
				}
				flag = flag2;
			}
			return flag;
		});
	}

	private static ITypeSymbol MemberType(ISymbol s)
	{
		if (!(s is IFieldSymbol fieldSymbol))
		{
			return ((IPropertySymbol)s).Type;
		}
		return fieldSymbol.Type;
	}

	private int ModStructSize(INamedTypeSymbol t)
	{
		int num = 0;
		int num2 = 1;
		foreach (ISymbol item in ModStructFields(t))
		{
			int num3 = Map(MemberType(item))?.Size ?? 8;
			int num4 = Math.Clamp(num3, 1, 8);
			num = (num + num4 - 1) / num4 * num4 + num3;
			num2 = Math.Max(num2, num4);
		}
		return Math.Max(1, (num + num2 - 1) / num2 * num2);
	}

	public INamedTypeSymbol? NativeBase(INamedTypeSymbol t)
	{
		for (INamedTypeSymbol baseType = t.BaseType; baseType != null; baseType = baseType.BaseType)
		{
			if (ClassPathAttr(baseType) != null)
			{
				return baseType;
			}
		}
		return null;
	}

	public UFunctionInfo? Function(IMethodSymbol m)
	{
		AttributeData attributeData = Attr(m.OriginalDefinition, "UFunctionAttribute");
		if (attributeData == null)
		{
			return null;
		}
		return new UFunctionInfo(ClassPathAttr(m.ContainingType) ?? throw new InvalidOperationException($"{m.ContainingType} has no UClass"), (string)attributeData.ConstructorArguments[0].Value, (uint)attributeData.ConstructorArguments[1].Value, UFunctionInfo.ParseSignature((string)attributeData.ConstructorArguments[2].Value), IsScriptOnly(m.OriginalDefinition));
	}

	public (string Name, UType Type, string Owner)? Property(ISymbol s)
	{
		AttributeData attributeData = Attr(s, "UPropertyAttribute");
		if (attributeData == null)
		{
			if (s is IFieldSymbol { AssociatedSymbol: IPropertySymbol associatedSymbol })
			{
				s = associatedSymbol;
			}
			INamedTypeSymbol containingType = s.ContainingType;
			if (containingType != null && IsModStruct(containingType) && ModStructFields(s.ContainingType).Contains<ISymbol>(s, SymbolEqualityComparer.Default))
			{
				UType uType = Map(MemberType(s));
				if ((object)uType != null)
				{
					return (UnrealName(s), uType, ModStructPath(s.ContainingType));
				}
			}
			return null;
		}
		INamedTypeSymbol containingType2 = s.ContainingType;
		string item = ((attributeData.ConstructorArguments.Length > 2 && attributeData.ConstructorArguments[2].Value is string text) ? text : ((containingType2.TypeKind == TypeKind.Struct) ? StructPath(containingType2) : ClassPathAttr(containingType2)));
		return ((string)attributeData.ConstructorArguments[0].Value, UType.Parse((string)attributeData.ConstructorArguments[1].Value), item);
	}

	public static string? StructPath(ITypeSymbol t)
	{
		return Attr(t, "UStructAttribute")?.ConstructorArguments[0].Value as string;
	}

	public static int StructSize(ITypeSymbol t)
	{
		object obj = Attr(t, "UStructAttribute")?.ConstructorArguments[1].Value;
		if (obj is int)
		{
			return (int)obj;
		}
		return 0;
	}

	public static string? EnumPath(ITypeSymbol t)
	{
		return Attr(t, "UEnumAttribute")?.ConstructorArguments[0].Value as string;
	}

	public static string? DelegatePath(ITypeSymbol t)
	{
		return Attr(t, "UDelegateAttribute")?.ConstructorArguments[0].Value as string;
	}

	public UType? Map(ITypeSymbol? t)
	{
		if (t == null)
		{
			return null;
		}
		switch (t.SpecialType)
		{
		case SpecialType.System_Boolean:
			return UType.Bool;
		case SpecialType.System_SByte:
			return UType.Int8;
		case SpecialType.System_Byte:
			return UType.Byte;
		case SpecialType.System_Int16:
			return UType.Int16;
		case SpecialType.System_UInt16:
			return UType.UInt16;
		case SpecialType.System_Int32:
			return UType.Int;
		case SpecialType.System_UInt32:
			return UType.UInt32;
		case SpecialType.System_Int64:
			return UType.Int64;
		case SpecialType.System_UInt64:
			return UType.UInt64;
		case SpecialType.System_Single:
			return UType.Float;
		case SpecialType.System_Double:
			return UType.Double;
		case SpecialType.System_String:
			return UType.String;
		default:
		{
			if (!(t is INamedTypeSymbol namedTypeSymbol))
			{
				return null;
			}
			if (namedTypeSymbol.ContainingNamespace?.ToDisplayString() == "NeoRune")
			{
				switch (namedTypeSymbol.MetadataName)
				{
				case "FName":
					return UType.Name;
				case "FText":
					return UType.Text;
				case "TSubclassOf`1":
				{
					string text4 = ClassPath(namedTypeSymbol.TypeArguments[0]);
					if (text4 == null)
					{
						return null;
					}
					return new UType.Class(text4);
				}
				case "TSoftObjectPtr`1":
				{
					string text2 = ClassPath(namedTypeSymbol.TypeArguments[0]);
					if (text2 == null)
					{
						return null;
					}
					return new UType.SoftObject(text2);
				}
				case "TSoftClassPtr`1":
				{
					string text3 = ClassPath(namedTypeSymbol.TypeArguments[0]);
					if (text3 == null)
					{
						return null;
					}
					return new UType.SoftClass(text3);
				}
				case "TWeakObjectPtr`1":
				{
					string text = ClassPath(namedTypeSymbol.TypeArguments[0]);
					if (text == null)
					{
						return null;
					}
					return new UType.WeakObject(text);
				}
				}
			}
			INamedTypeSymbol originalDefinition = namedTypeSymbol.OriginalDefinition;
			if (SymbolEqualityComparer.Default.Equals(originalDefinition, List))
			{
				UType uType = Map(namedTypeSymbol.TypeArguments[0]);
				if ((object)uType == null)
				{
					return null;
				}
				return new UType.Array(uType);
			}
			if (SymbolEqualityComparer.Default.Equals(originalDefinition, HashSet))
			{
				UType uType2 = Map(namedTypeSymbol.TypeArguments[0]);
				if ((object)uType2 == null)
				{
					return null;
				}
				return new UType.Set(uType2);
			}
			if (SymbolEqualityComparer.Default.Equals(originalDefinition, Dictionary))
			{
				UType uType3 = Map(namedTypeSymbol.TypeArguments[0]);
				if ((object)uType3 != null)
				{
					UType uType4 = Map(namedTypeSymbol.TypeArguments[1]);
					if ((object)uType4 != null)
					{
						return new UType.Map(uType3, uType4);
					}
				}
				return null;
			}
			if (namedTypeSymbol.TypeKind == TypeKind.Enum)
			{
				string text5 = EnumPath(namedTypeSymbol);
				if (text5 != null)
				{
					return new UType.Enum(text5);
				}
				// A user enum the mod generates ([Asset]): a byte property with the enum, like Blueprint enums.
				string package = AssetPackage(namedTypeSymbol);
				if (package != null && IsSource(namedTypeSymbol))
				{
					return new UType.Enum($"{package}.{AssetName(package)}");
				}
			}
			if (namedTypeSymbol.TypeKind == TypeKind.Enum)
			{
				INamedTypeSymbol enumUnderlyingType = namedTypeSymbol.EnumUnderlyingType;
				if (enumUnderlyingType != null)
				{
					return Map(enumUnderlyingType);
				}
			}
			if (namedTypeSymbol.TypeKind == TypeKind.Struct)
			{
				string text6 = StructPath(namedTypeSymbol);
				if (text6 != null)
				{
					return new UType.Struct(text6, StructSize(namedTypeSymbol));
				}
			}
			if (IsModStruct(namedTypeSymbol))
			{
				return new UType.Struct(ModStructPath(namedTypeSymbol), ModStructSize(namedTypeSymbol));
			}
			if (namedTypeSymbol.TypeKind == TypeKind.Delegate)
			{
				string text7 = DelegatePath(namedTypeSymbol);
				if (text7 != null)
				{
					return new UType.Delegate(text7);
				}
			}
			TypeKind typeKind = namedTypeSymbol.TypeKind;
			if ((typeKind == TypeKind.Class || typeKind == TypeKind.Interface) ? true : false)
			{
				string text8 = ClassPath(namedTypeSymbol);
				if (text8 != null)
				{
					return new UType.Object(text8);
				}
			}
			return null;
		}
		}
	}
}
