using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NeoRuneExtended.Discovery;

namespace NeoRuneExtended.Packaging;

public sealed class ModPackager
{
	public string ToolsDirectory { get; }

	public ModPackager(string toolsDirectory)
	{
		ToolsDirectory = toolsDirectory;
		string[] array = new string[2] { "retoc.exe", "repak.exe" };
		foreach (string text in array)
		{
			if (!File.Exists(Path.Combine(toolsDirectory, text)))
			{
				throw new FileNotFoundException(text + " was not found in " + toolsDirectory);
			}
		}
	}

	public string Pack(string modName, string assetsDirectory, string outputDirectory, string tempDirectory)
	{
		string path = Path.Combine("Dungeons", "Content", "Mods", modName);
		string text = Path.Combine(tempDirectory, "Staging");
		string text2 = Path.Combine(tempDirectory, "Header");
		string[] array = new string[2] { text, text2 };
		foreach (string path2 in array)
		{
			if (Directory.Exists(path2))
			{
				Directory.Delete(path2, recursive: true);
			}
		}
		string text3 = Path.Combine(text, path);
		Directory.CreateDirectory(text3);
		foreach (string item in from f in Directory.EnumerateFiles(assetsDirectory)
			where f.EndsWith(".uasset") || f.EndsWith(".uexp")
			select f)
		{
			File.Copy(item, Path.Combine(text3, Path.GetFileName(item)));
		}
		// Packages at fixed paths ([Asset]): Content\<path under /Game>, mounted at the game's Content.
		string content = Path.Combine(assetsDirectory, "Content");
		if (Directory.Exists(content))
		{
			foreach (string item2 in Directory.EnumerateFiles(content, "*", SearchOption.AllDirectories).Where((string f) => f.EndsWith(".uasset") || f.EndsWith(".uexp")))
			{
				string target = Path.Combine(text, "Dungeons", "Content", Path.GetRelativePath(content, item2));
				Directory.CreateDirectory(Path.GetDirectoryName(target));
				File.Copy(item2, target);
			}
		}
		if (!File.Exists(Path.Combine(text3, "ModActor.uasset")))
		{
			throw new InvalidOperationException("the mod has no ModActor asset");
		}
		string text4 = Path.Combine(text2, path);
		Directory.CreateDirectory(text4);
		array = new string[2] { "ModActor.uasset", "ModInfo.uasset" };
		foreach (string path3 in array)
		{
			if (File.Exists(Path.Combine(text3, path3)))
			{
				File.Copy(Path.Combine(text3, path3), Path.Combine(text4, path3));
			}
		}
		Directory.CreateDirectory(outputDirectory);
		string text5 = Path.Combine(outputDirectory, modName + "_P");
		array = new string[3] { ".pak", ".utoc", ".ucas" };
		foreach (string text6 in array)
		{
			if (File.Exists(text5 + text6))
			{
				File.Delete(text5 + text6);
			}
		}
		Run("retoc.exe", "to-zen", "--version", "UE5_6", text, text5 + ".utoc");
		File.Delete(text5 + ".pak");
		Run("repak.exe", "pack", "-q", "--version", "V11", text2, text5 + ".pak");
		return text5;
	}

	public static string Install(string stem, string modName, string gameDirectory)
	{
		if (GameInstall.IsRunning())
		{
			throw new InvalidOperationException("Close Minecraft Dungeons II first: it locks the files in ~mods.");
		}
		string text = Path.Combine(GameInstall.ModsDirectory(gameDirectory), modName);
		Directory.CreateDirectory(text);
		string[] array = new string[3] { ".pak", ".utoc", ".ucas" };
		foreach (string text2 in array)
		{
			File.Copy(stem + text2, Path.Combine(text, Path.GetFileName(stem) + text2), overwrite: true);
		}
		return text;
	}

	public static string ResolveGame(string? configured)
	{
		if (configured != null && configured.Length > 0)
		{
			if (!GameInstall.IsGame(configured))
			{
				throw new InvalidOperationException("NeoRuneGameDir \"" + configured + "\" is not a Minecraft Dungeons II folder: it must contain Dungeons\\Content\\Paks.");
			}
			return configured;
		}
		return GameInstall.Find() ?? throw new InvalidOperationException("Minecraft Dungeons II was not found in the Steam or Xbox app libraries. Set NeoRuneGameDir in the .csproj to the folder that contains \"Dungeons\" (for example D:\\XboxGames\\Minecraft Dungeons II\\Content), or run \"neorune doctor\" to see where NeoRune looked.");
	}

	private void Run(string tool, params string[] args)
	{
		ProcessStartInfo processStartInfo = new ProcessStartInfo(Path.Combine(ToolsDirectory, tool))
		{
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false
		};
		foreach (string item in args)
		{
			processStartInfo.ArgumentList.Add(item);
		}
		using Process process = Process.Start(processStartInfo);
		Task<string> task = process.StandardOutput.ReadToEndAsync();
		string value = process.StandardError.ReadToEnd();
		process.WaitForExit();
		if (process.ExitCode != 0)
		{
			throw new InvalidOperationException($"{tool} failed ({process.ExitCode}): {value}{task.Result}");
		}
	}
}
