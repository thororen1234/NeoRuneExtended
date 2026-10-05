using System;
using System.IO;
using NeoRuneExtended.Packaging;
using Newtonsoft.Json;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;

namespace NeoRuneExtended.Cli;

internal static class DevCommands
{
	public static int Inspect(Args a)
	{
		UAsset uAsset = new UAsset(a.Positional[0], EngineVersion.VER_UE5_6);
		Console.WriteLine($"flags {uAsset.PackageFlags}  unversioned={uAsset.HasUnversionedProperties} names={uAsset.GetNameMapIndexList().Count} imports={uAsset.Imports.Count} exports={uAsset.Exports.Count}");
		for (int i = 0; i < uAsset.Imports.Count; i++)
		{
			Import import = uAsset.Imports[i];
			Console.WriteLine($"  import {-(i + 1)}: {import.ClassPackage}.{import.ClassName} {import.ObjectName} outer={import.OuterIndex.Index}");
		}
		for (int j = 0; j < uAsset.Exports.Count; j++)
		{
			Export export = uAsset.Exports[j];
			Console.WriteLine($"  export {j + 1}: [{export.GetType().Name}] {export.ObjectName} class={export.ClassIndex.Index} super={export.SuperIndex.Index} template={export.TemplateIndex.Index} outer={export.OuterIndex.Index} flags={export.ObjectFlags}");
			if (!(export is NormalExport normalExport))
			{
				continue;
			}
			foreach (PropertyData datum in normalExport.Data)
			{
				Console.WriteLine($"      {datum.PropertyType} {datum.Name} = {datum}");
			}
		}
		return 0;
	}

	public static int Json(Args a)
	{
		UAsset uAsset = new UAsset(a.Positional[0], EngineVersion.VER_UE5_6);
		File.WriteAllText(a.Positional[1], uAsset.SerializeJson(Formatting.Indented));
		return 0;
	}

	public static int Roundtrip(Args a)
	{
		UAsset uAsset = new UAsset(a.Positional[0], EngineVersion.VER_UE5_6);
		string text = a.Positional[1];
		uAsset.Write(text);
		string[] array = new string[2] { ".uasset", ".uexp" };
		foreach (string text2 in array)
		{
			byte[] array2 = File.ReadAllBytes(Path.ChangeExtension(a.Positional[0], text2));
			byte[] array3 = File.ReadAllBytes(Path.ChangeExtension(text, text2));
			Console.WriteLine(text2 + ": " + (((ReadOnlySpan<byte>)array2.AsSpan()).SequenceEqual((ReadOnlySpan<byte>)array3) ? "identical" : $"differs ({array2.Length} vs {array3.Length} bytes)"));
		}
		return 0;
	}

	public static int Phase0(Args a)
	{
		string path = a.Require("out");
		string text = Path.Combine(path, "Assets", "NeoRuneHello");
		if (Directory.Exists(text))
		{
			Directory.Delete(text, recursive: true);
		}
		Hello.Build(text);
		string text2 = new ModPackager(a.Get("tools") ?? Path.Combine(AppContext.BaseDirectory, "tools")).Pack("NeoRuneHello", text, Path.Combine(path, "Build", "NeoRuneHello"), Path.Combine(path, "Temp", "NeoRuneHello"));
		Console.WriteLine("Packed " + text2);
		return 0;
	}
}
