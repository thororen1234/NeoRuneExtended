using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NeoRuneExtended.Assets;
using UAssetAPI.UnrealTypes;

namespace NeoRuneExtended.Compiler;

public sealed class ModCompiler
{
	private readonly Dictionary<INamedTypeSymbol, ClassCompiler> classes = new Dictionary<INamedTypeSymbol, ClassCompiler>(SymbolEqualityComparer.Default);

	private Dictionary<string, INamedTypeSymbol>? classesByPath;

	private HashSet<INamedTypeSymbol> sdkClasses = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

	private readonly List<INamedTypeSymbol> requested = new List<INamedTypeSymbol>();

	private readonly Dictionary<(string, string), UFunctionInfo?> libraryCache = new Dictionary<(string, string), UFunctionInfo>();

	public string ModName { get; }

	public CSharpCompilation Compilation { get; }

	public Symbols Symbols { get; }

	public List<ModDiagnostic> Diagnostics { get; } = new List<ModDiagnostic>();

	public bool HasErrors => Diagnostics.Any((ModDiagnostic d) => d.Severity == "error");

	public ModInfoData? Info { get; set; }

	public ModCompiler(string modName, IEnumerable<string> sourceFiles, IEnumerable<string> references, IEnumerable<string>? preprocessorSymbols = null)
	{
		ModName = modName;
		CSharpParseOptions options = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse, SourceCodeKind.Regular, preprocessorSymbols);
		IEnumerable<SyntaxTree> syntaxTrees = sourceFiles.Select((string f) => CSharpSyntaxTree.ParseText(File.ReadAllText(f), options, f));
		Compilation = CSharpCompilation.Create(modName, syntaxTrees, references.Select((string r) => MetadataReference.CreateFromFile(r)), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, reportSuppressedDiagnostics: false, null, null, null, null, OptimizationLevel.Debug, checkOverflow: false, allowUnsafe: false, null, null, default(ImmutableArray<byte>), null, Platform.AnyCpu, ReportDiagnostic.Default, 4, null, concurrentBuild: true, deterministic: false, null, null, null, null, null, publicSign: false, MetadataImportOptions.Public, NullableContextOptions.Enable));
		Symbols = new Symbols(Compilation, "/Game/Mods/" + modName);
	}

	public bool Compile(string outputDirectory)
	{
		foreach (Diagnostic item in from d in Compilation.GetDiagnostics()
			where d.Severity == DiagnosticSeverity.Error
			select d)
		{
			Diagnostics.Add(ModDiagnostic.From("error", item.GetMessage(), item.Location));
		}
		if (HasErrors)
		{
			return false;
		}
		List<INamedTypeSymbol> source = (from namedTypeSymbol in Compilation.SyntaxTrees.SelectMany((SyntaxTree syntaxTree) => from c in syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
				select Compilation.GetSemanticModel(syntaxTree).GetDeclaredSymbol(c)).OfType<INamedTypeSymbol>().Distinct((IEqualityComparer<INamedTypeSymbol>?)SymbolEqualityComparer.Default)
			where !namedTypeSymbol.IsAbstract && !namedTypeSymbol.IsStatic && Symbols.NativeBase(namedTypeSymbol) != null
			select namedTypeSymbol).ToList();
		if (!source.Any((INamedTypeSymbol namedTypeSymbol) => namedTypeSymbol.Name == "ModActor"))
		{
			Diagnostics.Add(ModDiagnostic.From("error", "the mod needs a class named ModActor deriving from AActor (Blueprint Loader spawns it)", null));
			return false;
		}
		sdkClasses = source.Where(IsSdkClass).ToHashSet((IEqualityComparer<INamedTypeSymbol>?)SymbolEqualityComparer.Default);
		requested.Clear();
		Symbols.SourceClassUsed += delegate(INamedTypeSymbol namedTypeSymbol)
		{
			if (sdkClasses.Contains(namedTypeSymbol.OriginalDefinition) && !classes.ContainsKey(namedTypeSymbol.OriginalDefinition) && !requested.Contains(namedTypeSymbol.OriginalDefinition, SymbolEqualityComparer.Default))
			{
				requested.Add(namedTypeSymbol.OriginalDefinition);
			}
		};
		foreach (INamedTypeSymbol t in source.Where((INamedTypeSymbol item) => !sdkClasses.Contains(item)))
		{
			Run(t, delegate
			{
				classes[t] = new ClassCompiler(this, t);
			});
		}
		if (HasErrors)
		{
			return false;
		}
		foreach (ClassCompiler item2 in classes.Values.ToList())
		{
			Run(item2.Type, item2.Declare);
		}
		while (!HasErrors)
		{
			bool flag = false;
			while (requested.Count > 0)
			{
				INamedTypeSymbol t2 = requested[0];
				requested.RemoveAt(0);
				if (!classes.ContainsKey(t2))
				{
					Run(t2, delegate
					{
						classes[t2] = new ClassCompiler(this, t2);
					});
					if (classes.TryGetValue(t2, out ClassCompiler value))
					{
						Run(t2, value.Declare);
					}
					RequestCompanions(t2);
					flag = true;
				}
			}
			if (!(from c in classes.Values.ToList()
				select c.Drain()).ToList().Any((bool drained) => drained) && !flag)
			{
				break;
			}
		}
		if (HasErrors)
		{
			return false;
		}
		List<UserStructBuilder> list = ModStructs();
		INamedTypeSymbol modActor = source.First((INamedTypeSymbol namedTypeSymbol) => namedTypeSymbol.Name == "ModActor");
		List<ModSettingData> settings = new List<ModSettingData>();
		Run(modActor, delegate
		{
			settings = ModSettings.Read(this, modActor);
		});
		if (HasErrors)
		{
			return false;
		}
		if (settings.Any((ModSettingData s) => s.Id != null) && !modActor.AllInterfaces.Any((INamedTypeSymbol i) => i.Name == "IModSettings" && IsSdkClass(i)))
		{
			Diagnostics.Add(ModDiagnostic.From("warning", "ModActor has settings but doesn't implement IModSettings, so it never gets their values", modActor.Locations.FirstOrDefault()));
		}
		if (Directory.Exists(outputDirectory))
		{
			Directory.Delete(outputDirectory, recursive: true);
		}
		foreach (ClassCompiler value2 in classes.Values)
		{
			value2.Blueprint.Write(AssetDirectory(value2.Blueprint.Package.PackageName, outputDirectory));
		}
		foreach (UserStructBuilder item3 in list)
		{
			item3.Write(AssetDirectory(item3.Package.PackageName, outputDirectory));
		}
		foreach (UserEnumBuilder item4 in ModEnums())
		{
			item4.Write(AssetDirectory(item4.Package.PackageName, outputDirectory));
		}
		foreach (BlueprintBuilder item5 in ModInterfaces())
		{
			item5.Write(AssetDirectory(item5.Package.PackageName, outputDirectory));
		}
		if (HasErrors)
		{
			return false;
		}
		if (Info != null)
		{
			ModInfoBuilder.Write(Symbols.ModRoot, new ModInfoData
			{
				ModName = Info.ModName,
				Version = Info.Version,
				Author = Info.Author,
				AuthorUrl = Info.AuthorUrl,
				Description = Info.Description,
				Settings = settings
			}, outputDirectory);
		}
		return true;
	}

	private static bool IsSdkClass(INamedTypeSymbol t)
	{
		return t.ContainingNamespace?.ToDisplayString() == "NeoRune";
	}

	private List<UserStructBuilder> ModStructs()
	{
		List<UserStructBuilder> result = new List<UserStructBuilder>();
		foreach (INamedTypeSymbol t in Compilation.SyntaxTrees.SelectMany((SyntaxTree syntaxTree) => from s in syntaxTree.GetRoot().DescendantNodes().OfType<StructDeclarationSyntax>()
			select Compilation.GetSemanticModel(syntaxTree).GetDeclaredSymbol(s)).OfType<INamedTypeSymbol>().Distinct((IEqualityComparer<INamedTypeSymbol>?)SymbolEqualityComparer.Default))
		{
			Run(t, delegate
			{
				IMethodSymbol methodSymbol = t.GetMembers().OfType<IMethodSymbol>().FirstOrDefault(delegate(IMethodSymbol m)
				{
					bool flag = !m.IsImplicitlyDeclared;
					if (flag)
					{
						MethodKind methodKind = m.MethodKind;
						bool flag2 = (uint)(methodKind - 11) <= 1u;
						flag = !flag2;
					}
					return flag;
				});
				if (methodSymbol != null)
				{
					throw new CompileError("struct " + t.Name + " can only hold fields: methods and constructors aren't supported in mod structs (put them in a class)", methodSymbol.Locations.FirstOrDefault());
				}
				string package = Symbols.ModPackage(t);
				string guidText = NeoRuneExtended.Compiler.Symbols.AssetGuid(t);
				Guid? guid = null;
				if (guidText != null)
				{
					guid = (Guid.TryParse(guidText, out Guid parsed) ? parsed : throw new CompileError("[Asset] Guid '" + guidText + "' of " + t.Name + " is not a GUID", t.Locations.FirstOrDefault()));
				}
				UserStructBuilder userStructBuilder = new UserStructBuilder(package, NeoRuneExtended.Compiler.Symbols.AssetName(package), guid);
				foreach (ISymbol item in NeoRuneExtended.Compiler.Symbols.ModStructFields(t))
				{
					ITypeSymbol typeSymbol = ((item is IFieldSymbol fieldSymbol) ? fieldSymbol.Type : ((IPropertySymbol)item).Type);
					userStructBuilder.AddField(NeoRuneExtended.Compiler.Symbols.UnrealName(item), Symbols.Map(typeSymbol) ?? throw new CompileError($"type '{typeSymbol}' of '{item.Name}' has no Unreal equivalent", item.Locations.FirstOrDefault()));
				}
				result.Add(userStructBuilder);
			});
		}
		return result;
	}

	/// <summary>
	/// Where a package's files go: the mod's own packages in the output folder, and packages elsewhere ([Asset]) under its
	/// Content folder by their path under /Game, which the packager mounts at the game's Content.
	/// </summary>
	private string AssetDirectory(string package, string outputDirectory)
	{
		if (package.StartsWith(Symbols.ModRoot + "/", StringComparison.Ordinal))
		{
			return outputDirectory;
		}
		if (!package.StartsWith("/Game/", StringComparison.Ordinal))
		{
			throw new CompileError("[Asset] path " + package + " must be under /Game/", null);
		}
		string folder = package.Substring("/Game/".Length);
		folder = folder.Substring(0, Math.Max(0, folder.LastIndexOf('/')));
		return Path.Combine(outputDirectory, "Content", folder.Replace('/', Path.DirectorySeparatorChar));
	}

	private IEnumerable<INamedTypeSymbol> SourceTypes(TypeKind kind)
	{
		return Compilation.SyntaxTrees.SelectMany((SyntaxTree syntaxTree) => from d in syntaxTree.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>()
			select Compilation.GetSemanticModel(syntaxTree).GetDeclaredSymbol(d)).OfType<INamedTypeSymbol>().Where((INamedTypeSymbol t) => t.TypeKind == kind).Distinct((IEqualityComparer<INamedTypeSymbol>?)SymbolEqualityComparer.Default);
	}

	/// <summary>The mod's [Asset] enums, as user enums: each value under its [UName] or its own name, numbered as in C#.</summary>
	private List<UserEnumBuilder> ModEnums()
	{
		List<UserEnumBuilder> result = new List<UserEnumBuilder>();
		foreach (INamedTypeSymbol t in SourceTypes(TypeKind.Enum))
		{
			string package = NeoRuneExtended.Compiler.Symbols.AssetPackage(t);
			if (package == null)
			{
				continue;
			}
			Run(t, delegate
			{
				UserEnumBuilder builder = new UserEnumBuilder(package, NeoRuneExtended.Compiler.Symbols.AssetName(package));
				foreach (IFieldSymbol item in t.GetMembers().OfType<IFieldSymbol>())
				{
					if (item.HasConstantValue)
					{
						builder.AddValue(NeoRuneExtended.Compiler.Symbols.UnrealName(item), Convert.ToInt64(item.ConstantValue));
					}
				}
				result.Add(builder);
			});
		}
		return result;
	}

	/// <summary>
	/// The mod's [Asset] interfaces, as Blueprint interfaces: a function for each method, with its parameters and an empty
	/// body, flagged like the Blueprint editor's (public, callable, implementable). Implement them on a class like any
	/// interface with [UClass].
	/// </summary>
	private List<BlueprintBuilder> ModInterfaces()
	{
		List<BlueprintBuilder> result = new List<BlueprintBuilder>();
		foreach (INamedTypeSymbol t in SourceTypes(TypeKind.Interface))
		{
			string package = NeoRuneExtended.Compiler.Symbols.AssetPackage(t);
			if (package == null)
			{
				continue;
			}
			Run(t, delegate
			{
				BlueprintBuilder builder = new BlueprintBuilder(package, NeoRuneExtended.Compiler.Symbols.AssetName(package), "/Script/CoreUObject.Interface");
				builder.MakeInterface();
				foreach (IMethodSymbol item in from m in t.GetMembers().OfType<IMethodSymbol>()
					where m.MethodKind == MethodKind.Ordinary
					select m)
				{
					if (!item.ReturnsVoid)
					{
						throw new CompileError("interface methods can't return a value (Blueprint interface events have none): " + item.Name, item.Locations.FirstOrDefault());
					}
					FunctionBuilder function = builder.AddFunction(NeoRuneExtended.Compiler.Symbols.UnrealName(item), EFunctionFlags.FUNC_Public | EFunctionFlags.FUNC_BlueprintCallable | EFunctionFlags.FUNC_BlueprintEvent);
					foreach (IParameterSymbol parameter in item.Parameters)
					{
						function.AddParameter(parameter.Name, Symbols.Map(parameter.Type) ?? throw new CompileError($"parameter type '{parameter.Type}' has no Unreal equivalent", parameter.Locations.FirstOrDefault()), parameter.RefKind != RefKind.None);
					}
				}
				result.Add(builder);
			});
		}
		return result;
	}

	public void Run(INamedTypeSymbol where, Action action)
	{
		try
		{
			action();
		}
		catch (CompileError compileError) when (compileError.AlreadyReported)
		{
		}
		catch (CompileError compileError2)
		{
			Diagnostics.Add(ModDiagnostic.From("error", compileError2.Message, compileError2.Location ?? where.Locations.FirstOrDefault()));
		}
	}

	public void Report(CompileError e)
	{
		if (!e.AlreadyReported)
		{
			Diagnostics.Add(ModDiagnostic.From("error", e.Message, e.Location));
		}
	}

	private void RequestCompanions(INamedTypeSymbol t)
	{
		foreach (AttributeData item in from a in t.GetAttributes()
			where a.AttributeClass?.Name == "CompileWithAttribute"
			select a)
		{
			if (item.ConstructorArguments[0].Value is string text)
			{
				INamedTypeSymbol typeByMetadataName = Compilation.GetTypeByMetadataName(t.ContainingNamespace.ToDisplayString() + "." + text);
				if (typeByMetadataName != null && sdkClasses.Contains(typeByMetadataName) && !classes.ContainsKey(typeByMetadataName) && !requested.Contains(typeByMetadataName, SymbolEqualityComparer.Default))
				{
					requested.Add(typeByMetadataName);
				}
			}
		}
	}

	internal ClassCompiler ClassFor(INamedTypeSymbol t)
	{
		t = t.OriginalDefinition;
		if (classes.TryGetValue(t, out ClassCompiler value))
		{
			return value;
		}
		if (!sdkClasses.Contains(t))
		{
			throw new CompileError(t.Name + " is not a mod class", t.Locations.FirstOrDefault());
		}
		ClassCompiler classCompiler = (classes[t] = new ClassCompiler(this, t));
		value = classCompiler;
		value.Declare();
		RequestCompanions(t);
		return value;
	}

	public UFunctionInfo? Library(string className, string name)
	{
		if (libraryCache.TryGetValue((className, name), out UFunctionInfo value))
		{
			return value;
		}
		IMethodSymbol methodSymbol = (Compilation.GetTypeByMetadataName("UE.Engine." + className) ?? Compilation.GetTypeByMetadataName("UE.UMG." + className))?.GetMembers().OfType<IMethodSymbol>().FirstOrDefault((IMethodSymbol x) => string.Equals(Symbols.Function(x)?.Name, name, StringComparison.OrdinalIgnoreCase));
		return libraryCache[(className, name)] = ((methodSymbol != null) ? Symbols.Function(methodSymbol) : null);
	}

	public bool IsSubclass(string cls, string ancestor)
	{
		if (cls == ancestor)
		{
			return true;
		}
		if (classesByPath == null)
		{
			classesByPath = BuildClassIndex();
		}
		if (!classesByPath.TryGetValue(cls, out INamedTypeSymbol value))
		{
			return false;
		}
		for (INamedTypeSymbol baseType = value.BaseType; baseType != null; baseType = baseType.BaseType)
		{
			if (Symbols.ClassPath(baseType) == ancestor)
			{
				return true;
			}
		}
		return false;
	}

	private Dictionary<string, INamedTypeSymbol> BuildClassIndex()
	{
		Dictionary<string, INamedTypeSymbol> map = new Dictionary<string, INamedTypeSymbol>();
		walk(Compilation.GlobalNamespace);
		return map;
		void walk(INamespaceSymbol ns)
		{
			foreach (INamedTypeSymbol typeMember in ns.GetTypeMembers())
			{
				string text = Symbols.ClassPath(typeMember, use: false);
				if (text != null)
				{
					map.TryAdd(text, typeMember);
				}
			}
			foreach (INamespaceSymbol namespaceMember in ns.GetNamespaceMembers())
			{
				walk(namespaceMember);
			}
		}
	}
}
