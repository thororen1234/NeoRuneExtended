using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using NeoRuneExtended.Discovery;

namespace NeoRuneExtended.Bindings;

public static class BindingsProject
{
	public const string GameBuildFile = "NeoRune.Game.json";

	public static ApiDump LoadDump(string path)
	{
		if (!path.EndsWith(".gz"))
		{
			return ApiDump.Load(path);
		}
		string tempFileName = Path.GetTempFileName();
		using (GZipStream gZipStream = new GZipStream(File.OpenRead(path), CompressionMode.Decompress))
		{
			using FileStream destination = File.Create(tempFileName);
			gZipStream.CopyTo(destination);
		}
		try
		{
			return ApiDump.Load(tempFileName);
		}
		finally
		{
			File.Delete(tempFileName);
		}
	}

	public static List<string> Write(ApiDump dump, string projectDirectory, string abstractionsProject)
	{
		BindingsGenerator bindingsGenerator = new BindingsGenerator(dump);
		Dictionary<string, string> dictionary = bindingsGenerator.Generate();
		string text = Path.Combine(projectDirectory, "Generated");
		if (Directory.Exists(text))
		{
			Directory.Delete(text, recursive: true);
		}
		Directory.CreateDirectory(text);
		foreach (var (path, contents) in dictionary)
		{
			File.WriteAllText(Path.Combine(text, path), contents);
		}
		File.WriteAllText(Path.Combine(projectDirectory, "NeoRune.Game.csproj"), $"<Project Sdk=\"Microsoft.NET.Sdk\">\r\n  <PropertyGroup>\r\n    <TargetFramework>netstandard2.0</TargetFramework>\r\n    <LangVersion>latest</LangVersion>\r\n    <Nullable>enable</Nullable>\r\n    <AssemblyName>NeoRune.Game</AssemblyName>\r\n    <Version>0.1.0</Version>\r\n    <InformationalVersion>0.1.0+build.{dump.Game.BuildId}</InformationalVersion>\r\n    <Description>NeoRune bindings for Minecraft Dungeons II build {dump.Game.BuildId} ({dump.Game.ExeStamp})</Description>\r\n    <ProduceReferenceAssembly>false</ProduceReferenceAssembly>\r\n    <NoWarn>CS0108;CS0114;CS0067;CS8618;CS1591;CS0109;CS0465;CS0660;CS0661</NoWarn>\r\n  </PropertyGroup>\r\n  <ItemGroup>\r\n    <ProjectReference Include=\"{abstractionsProject}\" />\r\n  </ItemGroup>\r\n</Project>");
		File.WriteAllText(Path.Combine(projectDirectory, "NeoRune.Game.json"), JsonSerializer.Serialize(dump.Game, ApiDump.Options));
		return bindingsGenerator.Warnings;
	}

	public static GameBuild? ReadGameBuild(string path)
	{
		try
		{
			return File.Exists(path) ? JsonSerializer.Deserialize<GameBuild>(File.ReadAllText(path), ApiDump.Options) : null;
		}
		catch (JsonException)
		{
			return null;
		}
	}
}
