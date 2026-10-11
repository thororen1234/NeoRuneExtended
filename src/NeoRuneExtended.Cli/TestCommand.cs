using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace NeoRuneExtended.Cli;

internal static class TestCommand
{
	public static int Run(Args a)
	{
		string? obj = PackCommand.FindProject(a.Get("project") ?? Environment.CurrentDirectory) ?? throw new InvalidOperationException("No mod project here: run it in the mod's folder, or pass --project <folder or .csproj>.");
		string directoryName = Path.GetDirectoryName(obj);
		string text = PackCommand.Properties(obj, "NeoRuneSdkRoot")["NeoRuneSdkRoot"];
		if (text.Length == 0 || !Directory.Exists(Path.Combine(text, "ref")))
		{
			throw new InvalidOperationException("Couldn't find the project's NeoRuneExtended.Sdk: build it once with dotnet build.");
		}
		List<TestResult> list = RunTests((from f in Directory.EnumerateFiles(directoryName, "*.cs", SearchOption.AllDirectories)
			where !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) && !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
			select f).Concat(Directory.EnumerateFiles(Path.Combine(text, "src"), "*.cs", SearchOption.AllDirectories)).ToList(), Path.Combine(text, "ref"), out List<string> errors);
		if (errors.Count > 0)
		{
			foreach (string item in errors)
			{
				Console.WriteLine(item);
			}
			Console.WriteLine("The mod doesn't compile as .NET code: fix the errors above.");
			return 1;
		}
		if (list.Count == 0)
		{
			Console.WriteLine("No tests: add a class marked [ModTests] whose public methods check your logic with ModAssert.");
			return 0;
		}
		foreach (TestResult item2 in list)
		{
			Console.WriteLine(item2.Passed ? ("  pass  " + item2.Name) : (item2.NeedsGame ? ("  game  " + item2.Name + ": " + item2.Message) : ("  FAIL  " + item2.Name + ": " + item2.Message)));
		}
		int num = list.Count((TestResult r) => !r.Passed && !r.NeedsGame);
		int num2 = list.Count((TestResult r) => r.NeedsGame);
		Console.WriteLine();
		Console.WriteLine($"{list.Count((TestResult r) => r.Passed)} passed, {num} failed{((num2 > 0) ? $", {num2} need the game (they call it: test those in game)" : "")}.");
		return (num > 0) ? 1 : 0;
	}

	internal static List<TestResult> RunTests(IReadOnlyList<string> sources, string refDir, out List<string> errors)
	{
		errors = new List<string>();
		CSharpParseOptions options = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Parse, SourceCodeKind.Regular, new string[1] { "NEORUNE_TESTS" });
		List<SyntaxTree> syntaxTrees = sources.Select((string f) => CSharpSyntaxTree.ParseText(File.ReadAllText(f), options, f)).ToList();
		IEnumerable<PortableExecutableReference> references = from r in Directory.EnumerateFiles(Path.GetDirectoryName(typeof(object).Assembly.Location), "*.dll").Where(IsManaged).Concat(new string[2]
			{
				Path.Combine(refDir, "NeoRune.Abstractions.dll"),
				Path.Combine(refDir, "NeoRune.Game.dll")
			})
			select MetadataReference.CreateFromFile(r);
		CSharpCompilation cSharpCompilation = CSharpCompilation.Create("ModTests_" + Guid.NewGuid().ToString("N"), syntaxTrees, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, reportSuppressedDiagnostics: false, null, null, null, null, OptimizationLevel.Debug, checkOverflow: false, allowUnsafe: false, null, null, default(ImmutableArray<byte>), null, Platform.AnyCpu, ReportDiagnostic.Default, 4, null, concurrentBuild: true, deterministic: false, null, null, null, null, null, publicSign: false, MetadataImportOptions.Public, NullableContextOptions.Enable));
		using MemoryStream memoryStream = new MemoryStream();
		EmitResult emitResult = cSharpCompilation.Emit(memoryStream);
		if (!emitResult.Success)
		{
			errors.AddRange(from d in emitResult.Diagnostics
				where d.Severity == DiagnosticSeverity.Error
				select d.ToString());
			return new List<TestResult>();
		}
		AssemblyLoadContext assemblyLoadContext = new AssemblyLoadContext("neorune-test", isCollectible: true);
		assemblyLoadContext.Resolving += delegate(AssemblyLoadContext ctx, AssemblyName assemblyName)
		{
			string text = Path.Combine(refDir, assemblyName.Name + ".dll");
			return (!File.Exists(text)) ? null : ctx.LoadFromAssemblyPath(text);
		};
		memoryStream.Position = 0L;
		Assembly assembly = assemblyLoadContext.LoadFromStream(memoryStream);
		List<TestResult> list = new List<TestResult>();
		try
		{
			foreach (Type item in from t in assembly.GetTypes()
				where t.GetCustomAttributes(inherit: false).Any((object x) => x.GetType().Name == "ModTestsAttribute")
				orderby t.Name
				select t)
			{
				object obj = null;
				foreach (MethodInfo item2 in from m in item.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
					where m.GetParameters().Length == 0 && !m.IsSpecialName && !m.IsGenericMethodDefinition
					orderby m.MetadataToken
					select m)
				{
					string name = item.Name + "." + item2.Name;
					try
					{
						if (!item2.IsStatic && obj == null)
						{
							obj = Activator.CreateInstance(item);
						}
						item2.Invoke(item2.IsStatic ? null : obj, null);
						list.Add(new TestResult(name, Passed: true, NeedsGame: false, ""));
					}
					catch (Exception ex)
					{
						Exception ex2;
						if (ex is TargetInvocationException)
						{
							Exception innerException = ex.InnerException;
							if (innerException != null)
							{
								ex2 = innerException;
								goto IL_034f;
							}
						}
						ex2 = ex;
						goto IL_034f;
						IL_034f:
						Exception e = ex2;
						list.Add(Describe(name, e));
					}
				}
			}
		}
		finally
		{
			assemblyLoadContext.Unload();
		}
		return list;
	}

	private static TestResult Describe(string name, Exception e)
	{
		if (e.GetType().Name == "ModAssertException")
		{
			return new TestResult(name, Passed: false, NeedsGame: false, e.Message);
		}
		MethodBase methodBase = (from f in new StackTrace(e).GetFrames()
			select f.GetMethod()).FirstOrDefault((MethodBase m) => m?.DeclaringType?.Assembly.GetName().Name == "NeoRune.Game");
		if (methodBase != null)
		{
			return new TestResult(name, Passed: false, NeedsGame: true, $"calls the game ({methodBase.DeclaringType.Name}.{methodBase.Name})");
		}
		if (e is InvalidOperationException && e.Message.Contains("only run inside the game"))
		{
			return new TestResult(name, Passed: false, NeedsGame: true, "uses a game-only feature (names, classes, the mod's name...)");
		}
		return new TestResult(name, Passed: false, NeedsGame: false, e.GetType().Name + ": " + e.Message);
	}

	private static bool IsManaged(string file)
	{
		try
		{
			AssemblyName.GetAssemblyName(file);
			return true;
		}
		catch (BadImageFormatException)
		{
			return false;
		}
	}
}
