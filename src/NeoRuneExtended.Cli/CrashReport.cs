using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace NeoRuneExtended.Cli;

internal sealed record CrashReport(string Folder, DateTime TimeUtc, string Error, string? CommandLine)
{
	public static string CrashesDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Dungeons2", "Saved", "Crashes");

	public static List<CrashReport> All(string? directory = null)
	{
		string path = directory ?? CrashesDirectory;
		if (!Directory.Exists(path))
		{
			return new List<CrashReport>();
		}
		return (from c in (from d in Directory.EnumerateDirectories(path, "UECC-*")
				select Read(d)).OfType<CrashReport>()
			orderby c.TimeUtc descending
			select c).ToList();
	}

	public static CrashReport? Read(string folder)
	{
		string path = Path.Combine(folder, "CrashContext.runtime-xml");
		if (!File.Exists(path))
		{
			return null;
		}
		string text;
		try
		{
			text = File.ReadAllText(path);
		}
		catch (IOException)
		{
			return null;
		}
		string? Tag(string name)
		{
			Match match = Regex.Match(text, $"<{name}>(.*?)</{name}>", RegexOptions.Singleline);
			if (match == null || !match.Success)
			{
				return null;
			}
			return WebUtility.HtmlDecode(match.Groups[1].Value).Trim();
		}
		DateTime timeUtc = ((long.TryParse(Tag("TimeOfCrash"), out var result) && result > 0 && result < DateTime.MaxValue.Ticks) ? new DateTime(result, DateTimeKind.Utc) : File.GetLastWriteTimeUtc(path));
		return new CrashReport(folder, timeUtc, Tag("ErrorMessage") ?? "", Tag("CommandLine"));
	}

	public (string Explanation, bool ModsCanCause) Explain()
	{
		string error = Error;
		if (error.Contains("GPU Crash", StringComparison.OrdinalIgnoreCase) || (error.Contains("D3D", StringComparison.Ordinal) && error.Contains("removed", StringComparison.OrdinalIgnoreCase)))
		{
			return (Explanation: "The graphics driver crashed (a GPU crash). Mods don't cause this: update the graphics driver or lower the graphics settings.", ModsCanCause: false);
		}
		Match match = Regex.Match(error, "Failed to find function (\\S+) in (\\S+)");
		if (match != null && match.Success)
		{
			return (Explanation: $"Script code called the function {match.Groups[1].Value} on {ShortObject(match.Groups[2].Value)}, which doesn't exist. " + "Usually a mod made for another version of the game: update or rebuild the mods that touch that screen.", ModsCanCause: true);
		}
		if (error.Contains("ScriptCore.cpp"))
		{
			return (Explanation: "The Blueprint script engine stopped on an error in script code (a mod's or the game's). The first line of the error says which.", ModsCanCause: true);
		}
		if (error.Contains("AsyncLoading", StringComparison.Ordinal))
		{
			return (Explanation: "A package failed to load. Usually a mod built for a different game build, or a damaged mod file: rebuild the mods with the NeoRune for this game build (\"neorunex doctor\" checks), or remove outdated ones.", ModsCanCause: true);
		}
		if (error.Contains("EXCEPTION_ACCESS_VIOLATION writing address 0x0000000000000457"))
		{
			return (Explanation: "A widget was added to one of the game's UI slots while the game was building that screen. Known for mods built with NeoRune before 0.5 that use GameUI.AddToSlot: rebuild them with the current NeoRune.", ModsCanCause: true);
		}
		if (error.Contains("EXCEPTION_ACCESS_VIOLATION reading address 0x00000000000000d8"))
		{
			return (Explanation: "Often a list compared with null or used with ?? in mod code (lists are never null in Unreal, and that comparison reads a list as an object). Check the mods' code for \"list == null\" or \"list ?? ...\".", ModsCanCause: true);
		}
		if (error.Contains("EXCEPTION_ACCESS_VIOLATION"))
		{
			return (Explanation: "The game read or wrote invalid memory. That can be the game itself or code it was running for a mod.", ModsCanCause: true);
		}
		if (error.Contains("LowLevelFatalError") || error.Contains("Fatal error"))
		{
			return (Explanation: "The engine stopped on a fatal error (the first line of the error says where).", ModsCanCause: true);
		}
		return (Explanation: "No explanation known for this error.", ModsCanCause: true);
	}

	private static string ShortObject(string path)
	{
		string[] array = path.Split('.', ':');
		if (array.Length < 1)
		{
			return path;
		}
		return array[^1];
	}
}
