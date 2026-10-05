using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace NeoRuneExtended.Discovery;

public static class GameInstall
{
	public const string FolderName = "Minecraft Dungeons II";

	public static readonly (string Platform, string Exe)[] Executables = new(string, string)[2]
	{
		("Win64", "Dungeons-Win64-Shipping.exe"),
		("WinGDK", "Dungeons-WinGDK-Shipping.exe")
	};

	public static (string Platform, string Path)? FindExecutable(string game)
	{
		(string, string)[] executables = Executables;
		for (int i = 0; i < executables.Length; i++)
		{
			(string, string) tuple = executables[i];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			string text = Path.Combine(game, "Dungeons", "Binaries", item, item2);
			if (File.Exists(text))
			{
				return (item, text);
			}
		}
		return null;
	}

	public static bool IsGdk(string game)
	{
		return FindExecutable(game)?.Platform == "WinGDK";
	}

	public static Process[] RunningProcesses()
	{
		return Executables.SelectMany(((string Platform, string Exe) e) => Process.GetProcessesByName(Path.GetFileNameWithoutExtension(e.Exe))).ToArray();
	}

	public static bool IsRunning()
	{
		return RunningProcesses().Length != 0;
	}

	public static string PaksDirectory(string game)
	{
		return Path.Combine(game, "Dungeons", "Content", "Paks");
	}

	public static string ModsDirectory(string game)
	{
		return Path.Combine(PaksDirectory(game), "~mods");
	}

	public static bool IsGame(string folder)
	{
		return Directory.Exists(PaksDirectory(folder));
	}

	public static string? Find()
	{
		return FindAll().FirstOrDefault();
	}

	public static List<string> FindAll()
	{
		List<string> list = new List<string>();
		foreach (string item in SteamCandidates().Concat(XboxCandidates()))
		{
			string fullPath;
			try
			{
				fullPath = Path.GetFullPath(item);
			}
			catch
			{
				continue;
			}
			if (IsGame(fullPath) && !list.Contains<string>(fullPath, StringComparer.OrdinalIgnoreCase))
			{
				list.Add(fullPath);
			}
		}
		return list;
	}

	public static IEnumerable<string> SearchedLocations()
	{
		return SteamCandidates().Concat(XboxCandidates()).Distinct<string>(StringComparer.OrdinalIgnoreCase);
	}

	private static IEnumerable<string> SteamCandidates()
	{
		List<string> list = new List<string> { "C:\\Program Files (x86)\\Steam", "C:\\Program Files\\Steam" };
		if (OperatingSystem.IsWindows())
		{
			(string, string)[] array = new(string, string)[3]
			{
				("HKEY_CURRENT_USER\\Software\\Valve\\Steam", "SteamPath"),
				("HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Valve\\Steam", "InstallPath"),
				("HKEY_LOCAL_MACHINE\\SOFTWARE\\Valve\\Steam", "InstallPath")
			};
			for (int i = 0; i < array.Length; i++)
			{
				var (keyName, valueName) = array[i];
				try
				{
					if (Registry.GetValue(keyName, valueName, null) is string { Length: >0 } text)
					{
						list.Insert(0, text.Replace('/', '\\'));
					}
				}
				catch
				{
				}
			}
		}
		List<string> list2 = new List<string>();
		foreach (string item in list.Distinct<string>(StringComparer.OrdinalIgnoreCase))
		{
			list2.Add(item);
			string path = Path.Combine(item, "steamapps", "libraryfolders.vdf");
			string input;
			try
			{
				if (!File.Exists(path))
				{
					continue;
				}
				input = File.ReadAllText(path);
				goto IL_0123;
			}
			catch
			{
			}
			continue;
			IL_0123:
			foreach (Match item2 in Regex.Matches(input, "\"path\"\\s+\"([^\"]+)\""))
			{
				list2.Add(item2.Groups[1].Value.Replace("\\\\", "\\"));
			}
		}
		return from l in list2.Distinct<string>(StringComparer.OrdinalIgnoreCase)
			select Path.Combine(l, "steamapps", "common", "Minecraft Dungeons II");
	}

	private static IEnumerable<string> XboxCandidates()
	{
		List<string> list = new List<string>();
		DriveInfo[] array;
		try
		{
			array = DriveInfo.GetDrives();
		}
		catch
		{
			array = Array.Empty<DriveInfo>();
		}
		DriveInfo[] array2 = array;
		foreach (DriveInfo driveInfo in array2)
		{
			bool flag;
			try
			{
				flag = driveInfo.DriveType == DriveType.Fixed && driveInfo.IsReady;
			}
			catch
			{
				flag = false;
			}
			if (!flag)
			{
				continue;
			}
			string root = driveInfo.RootDirectory.FullName;
			List<string> list2 = (from p in ReadGamingRoot(Path.Combine(root, ".GamingRoot"))
				select Path.Combine(root, p)).ToList();
			list2.AddRange(new string[2] { "XboxGames", "Xbox Games" }.Select((string n) => Path.Combine(root, n)));
			foreach (string item in list2.Distinct<string>(StringComparer.OrdinalIgnoreCase))
			{
				list.Add(Path.Combine(item, "Minecraft Dungeons II", "Content"));
				list.Add(Path.Combine(item, "Minecraft Dungeons II"));
			}
		}
		return list;
	}

	public static List<string> ReadGamingRoot(string path)
	{
		List<string> list = new List<string>();
		byte[] array;
		try
		{
			if (!File.Exists(path))
			{
				return list;
			}
			array = File.ReadAllBytes(path);
		}
		catch
		{
			return list;
		}
		if (array.Length < 8 || Encoding.ASCII.GetString(array, 0, 4) != "RGBX")
		{
			return list;
		}
		int num = BitConverter.ToInt32(array, 4);
		int num2 = 8;
		for (int i = 0; i < num; i++)
		{
			if (num2 + 1 >= array.Length)
			{
				break;
			}
			int j;
			for (j = num2; j + 1 < array.Length && (array[j] != 0 || array[j + 1] != 0); j += 2)
			{
			}
			string text = Encoding.Unicode.GetString(array, num2, j - num2).TrimStart('\\');
			if (text.Length > 0)
			{
				list.Add(text);
			}
			num2 = j + 2;
		}
		return list;
	}

	public static GameBuild Identify(string game)
	{
		GameBuild gameBuild = new GameBuild();
		(string, string)? tuple = FindExecutable(game);
		if (tuple.HasValue)
		{
			(string, string) valueOrDefault = tuple.GetValueOrDefault();
			try
			{
				gameBuild.ExeStamp = ExeStamp(valueOrDefault.Item2);
			}
			catch
			{
			}
			try
			{
				gameBuild.BuildId = SteamBuildId(valueOrDefault.Item2);
			}
			catch
			{
			}
		}
		try
		{
			gameBuild.GameVersion = PackageVersion(game);
		}
		catch
		{
		}
		return gameBuild;
	}

	public static bool? SameBuild(GameBuild a, GameBuild b)
	{
		if (a.ExeStamp.Length > 0 && b.ExeStamp.Length > 0 && a.ExeStamp == b.ExeStamp)
		{
			return true;
		}
		if (a.GameVersion.Length > 0 && b.GameVersion.Length > 0)
		{
			return a.GameVersion == b.GameVersion;
		}
		if (a.BuildId.Length > 0 && b.BuildId.Length > 0)
		{
			return a.BuildId == b.BuildId;
		}
		if (a.ExeStamp.Length > 0 && b.ExeStamp.Length > 0)
		{
			return false;
		}
		return null;
	}

	public static string Describe(GameBuild b)
	{
		List<string> list = new List<string>();
		if (b.GameVersion.Length > 0)
		{
			list.Add(b.GameVersion);
		}
		if (b.BuildId.Length > 0)
		{
			list.Add("Steam build " + b.BuildId);
		}
		if (list.Count == 0 && b.ExeStamp.Length > 0)
		{
			list.Add("exe " + b.ExeStamp);
		}
		if (list.Count != 0)
		{
			if (list.Count != 1)
			{
				return list[0] + " (" + list[1] + ")";
			}
			return list[0];
		}
		return "unknown build";
	}

	public static string ExeStamp(string exe)
	{
		byte[] array = new byte[1024];
		using (FileStream fileStream = File.OpenRead(exe))
		{
			fileStream.ReadExactly(array);
		}
		int num = BitConverter.ToInt32(array, 60);
		uint value = BitConverter.ToUInt32(array, num + 8);
		return $"{value:x8}-{new FileInfo(exe).Length:x}";
	}

	public static string SteamBuildId(string exe)
	{
		DirectoryInfo directoryInfo = new DirectoryInfo(Path.GetDirectoryName(exe));
		while (directoryInfo != null && !directoryInfo.Name.Equals("common", StringComparison.OrdinalIgnoreCase))
		{
			directoryInfo = directoryInfo.Parent;
		}
		DirectoryInfo directoryInfo2 = directoryInfo?.Parent;
		if (directoryInfo2 == null || !directoryInfo2.Exists)
		{
			return "";
		}
		FileInfo[] files = directoryInfo2.GetFiles("appmanifest_*.acf");
		for (int i = 0; i < files.Length; i++)
		{
			string text = File.ReadAllText(files[i].FullName);
			if (text.Contains("Minecraft Dungeons II"))
			{
				Match match = Regex.Match(text, "\"buildid\"\\s+\"(\\d+)\"");
				if (match.Success)
				{
					return match.Groups[1].Value;
				}
			}
		}
		return "";
	}

	public static string PackageVersion(string game)
	{
		string path = Path.Combine(game, "MicrosoftGame.config");
		if (!File.Exists(path))
		{
			return "";
		}
		Match match = Regex.Match(File.ReadAllText(path), "<Identity\\b[^>]*\\bVersion=\"([^\"]+)\"");
		if (!match.Success)
		{
			return "";
		}
		return match.Groups[1].Value;
	}
}
