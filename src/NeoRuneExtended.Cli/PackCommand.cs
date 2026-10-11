using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace NeoRuneExtended.Cli;

internal static class PackCommand
{
	public static int Run(Args a)
	{
		string text = FindProject(a.Get("project") ?? Environment.CurrentDirectory) ?? throw new InvalidOperationException("No mod project here: run it in the mod's folder, or pass --project <folder or .csproj>.");
		string directoryName = Path.GetDirectoryName(text);
		Dictionary<string, string> dictionary = Properties(text, "NeoRuneModName", "NeoRuneModTitle", "NeoRuneModVersion", "Version", "NeoRuneModAuthor", "NeoRuneModAuthorUrl", "NeoRuneModDescription", "NeoRuneOutDir");
		string text2 = dictionary["NeoRuneModName"];
		if (text2.Length == 0)
		{
			throw new InvalidOperationException(text + " is not a NeoRune mod project (no NeoRuneModName).");
		}
		string text3 = dictionary["NeoRuneModTitle"];
		string text4 = ((text3 != null && text3.Length > 0) ? text3 : text2);
		string text5 = dictionary["NeoRuneModVersion"];
		object obj;
		if (text5 == null || text5.Length <= 0)
		{
			string text6 = dictionary["Version"];
			obj = ((text6 != null && text6.Length > 0) ? text6 : "1.0");
		}
		else
		{
			obj = text5;
		}
		string text7 = (string)obj;
		Console.WriteLine($"Building {text4} {text7} in Release...");
		if (Run("dotnet", "build", text, "-c", "Release", "-nologo", "-v", "q", "-p:NeoRuneInstall=false", "-p:NeoRuneProfile=false", "-p:NeoRuneLiveTuning=false") != 0)
		{
			Console.WriteLine("The Release build failed: fix the errors above and run neorunex pack again.");
			return 1;
		}
		string text8 = dictionary["NeoRuneOutDir"];
		string text9 = Path.Combine((text8 != null && text8.Length > 0) ? text8 : Path.Combine(directoryName, "bin", "NeoRune"), "Pak", text2 + "_P");
		string[] array = new string[4] { ".pak", ".utoc", ".ucas", ".neorune.json" };
		foreach (string item in array.Take(3))
		{
			if (!File.Exists(text9 + item))
			{
				throw new InvalidOperationException(text9 + item + " is missing after the build.");
			}
		}
		if (File.Exists(text9 + ".neorune.json") && ModManifest.Read(text9 + ".neorune.json")?.Profile != null)
		{
			throw new InvalidOperationException("The build is still a profiling build (NeoRuneProfile is set in the project?): remove it before releasing.");
		}
		string text10 = (File.Exists(Path.Combine(directoryName, "README.txt")) ? File.ReadAllText(Path.Combine(directoryName, "README.txt")) : Readme(text4, text7, text2, dictionary["NeoRuneModAuthor"], dictionary["NeoRuneModAuthorUrl"], dictionary["NeoRuneModDescription"]));
		string? obj2 = a.Get("out") ?? Path.Combine(directoryName, "Release");
		Directory.CreateDirectory(obj2);
		string text11 = Path.Combine(obj2, FileName(text4) + "-" + text7 + ".zip");
		if (File.Exists(text11))
		{
			File.Delete(text11);
		}
		using (ZipArchive zipArchive = ZipFile.Open(text11, ZipArchiveMode.Create))
		{
			string[] array2 = array;
			foreach (string text12 in array2)
			{
				if (File.Exists(text9 + text12))
				{
					zipArchive.CreateEntryFromFile(text9 + text12, $"{text2}/{text2}_P{text12}", CompressionLevel.Optimal);
				}
			}
			using StreamWriter streamWriter = new StreamWriter(zipArchive.CreateEntry("README.txt").Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			streamWriter.Write(text10.Replace("\r\n", "\n").Replace("\n", "\r\n"));
		}
		Console.WriteLine("Packed " + text11);
		using (ZipArchive zipArchive2 = ZipFile.OpenRead(text11))
		{
			foreach (ZipArchiveEntry entry in zipArchive2.Entries)
			{
				Console.WriteLine($"  {entry.FullName,-40} {entry.Length,9:N0} bytes");
			}
		}
		Console.WriteLine("Upload it to Nexus as the main file. (Your installed copy is unchanged: dotnet build still installs the Debug build.)");
		return 0;
	}

	internal static string Readme(string title, string version, string mod, string author, string authorUrl, string description)
	{
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(1, 2, stringBuilder2);
		handler.AppendFormatted(title.ToUpperInvariant());
		handler.AppendLiteral(" ");
		handler.AppendFormatted(version);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder.AppendLine(new string('=', title.Length + version.Length + 1));
		if (author.Length > 0)
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder4 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(3, 2, stringBuilder2);
			handler.AppendLiteral("by ");
			handler.AppendFormatted(author);
			handler.AppendFormatted((authorUrl.Length > 0) ? (" (" + authorUrl + ")") : "");
			stringBuilder4.AppendLine(ref handler);
		}
		stringBuilder.AppendLine();
		if (description.Length > 0)
		{
			stringBuilder.AppendLine(description.Replace("\\n", "\n"));
			stringBuilder.AppendLine();
		}
		stringBuilder.AppendLine("INSTALL");
		stringBuilder.AppendLine("-------");
		stringBuilder.AppendLine("1. Install Blueprint Loader: https://www.nexusmods.com/minecraftdungeons2/mods/2");
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(67, 1, stringBuilder2);
		handler.AppendLiteral("2. Extract the \"");
		handler.AppendFormatted(mod);
		handler.AppendLiteral("\" folder from this zip into the game's mods folder:");
		stringBuilder5.AppendLine(ref handler);
		stringBuilder.AppendLine("     Steam:    ...\\steamapps\\common\\Minecraft Dungeons II\\Dungeons\\Content\\Paks\\~mods");
		stringBuilder.AppendLine("     Xbox app: ...\\Minecraft Dungeons II\\Content\\Dungeons\\Content\\Paks\\~mods");
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(72, 2, stringBuilder2);
		handler.AppendLiteral("   so that you have ~mods\\");
		handler.AppendFormatted(mod);
		handler.AppendLiteral("\\");
		handler.AppendFormatted(mod);
		handler.AppendLiteral("_P.pak (with its .utoc and .ucas next to it).");
		stringBuilder6.AppendLine(ref handler);
		stringBuilder.AppendLine("3. Start the game.");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("To uninstall, delete that folder.");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("Made with NeoRune (C# modding for Minecraft Dungeons II).");
		return stringBuilder.ToString();
	}

	private static string FileName(string title)
	{
		string text = new string(title.Where(delegate(char c)
		{
			bool flag = char.IsLetterOrDigit(c);
			if (!flag)
			{
				bool flag2 = ((c == '-' || c == '.' || c == '_') ? true : false);
				flag = flag2;
			}
			return flag;
		}).ToArray());
		if (text == null || text.Length <= 0)
		{
			return "Mod";
		}
		return text;
	}

	internal static string? FindProject(string path)
	{
		if (File.Exists(path) && path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
		{
			return Path.GetFullPath(path);
		}
		if (!Directory.Exists(path))
		{
			return null;
		}
		return Directory.EnumerateFiles(path, "*.csproj").FirstOrDefault((string f) => File.ReadAllText(f).Contains("NeoRuneExtended.Sdk"));
	}

	internal static Dictionary<string, string> Properties(string project, params string[] names)
	{
		List<string> list = new List<string>();
		list.Add("msbuild");
		list.Add(project);
		list.Add("-p:Configuration=Release");
		list.Add("-nologo");
		list.AddRange(names.Select((string n) => "-getProperty:" + n));
		ProcessStartInfo processStartInfo = new ProcessStartInfo("dotnet")
		{
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false
		};
		foreach (string item in list)
		{
			processStartInfo.ArgumentList.Add(item);
		}
		using Process process = Process.Start(processStartInfo);
		string text = process.StandardOutput.ReadToEnd();
		process.WaitForExit();
		if (process.ExitCode != 0)
		{
			throw new InvalidOperationException("Could not read the project's properties: " + text.Trim());
		}
		Dictionary<string, string> dictionary = names.ToDictionary((string n) => n, (string _) => "");
		using JsonDocument jsonDocument = JsonDocument.Parse(text);
		if (jsonDocument.RootElement.TryGetProperty("Properties", out var value))
		{
			foreach (string text2 in names)
			{
				if (value.TryGetProperty(text2, out var value2))
				{
					dictionary[text2] = value2.GetString() ?? "";
				}
			}
		}
		return dictionary;
	}

	private static int Run(string exe, params string[] args)
	{
		ProcessStartInfo processStartInfo = new ProcessStartInfo(exe)
		{
			UseShellExecute = false
		};
		foreach (string item in args)
		{
			processStartInfo.ArgumentList.Add(item);
		}
		using Process process = Process.Start(processStartInfo);
		process.WaitForExit();
		return process.ExitCode;
	}
}
