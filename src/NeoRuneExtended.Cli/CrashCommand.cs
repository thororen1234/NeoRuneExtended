using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using NeoRuneExtended.Discovery;

namespace NeoRuneExtended.Cli;

internal static class CrashCommand
{
	public static int Run(Args a)
	{
		List<CrashReport> list = CrashReport.All(a.Get("crashes"));
		if (list.Count == 0)
		{
			Console.WriteLine("No crash reports in " + CrashReport.CrashesDirectory + ".");
			return 0;
		}
		if (a.Flag("list"))
		{
			for (int i = 0; i < Math.Min(list.Count, 15); i++)
			{
				Console.WriteLine($"{i + 1,3}. {list[i].TimeUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss}  {FirstLine(list[i].Error)}");
			}
			Console.WriteLine("Details: neorunex crash <number>");
			return 0;
		}
		int num = ((a.Positional.Count > 0 && int.TryParse(a.Positional[0], out var result)) ? (result - 1) : 0);
		if (num < 0 || num >= list.Count)
		{
			throw new UsageException($"there are {list.Count} crash reports: pick 1 to {list.Count}");
		}
		CrashReport crashReport = list[num];
		Console.WriteLine($"Crash at {crashReport.TimeUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss} ({Ago(DateTime.UtcNow - crashReport.TimeUtc)} ago)");
		Console.WriteLine("  " + crashReport.Folder);
		Console.WriteLine();
		Console.WriteLine("Error");
		foreach (string item in (from l in crashReport.Error.Split('\n')
			select l.TrimEnd() into l
			where l.Length > 0
			select l).Take(4))
		{
			Console.WriteLine("  " + item);
		}
		var (text, flag) = crashReport.Explain();
		Console.WriteLine();
		Console.WriteLine("What it means");
		Console.WriteLine("  " + text);
		string text2 = GameInstall.FindAll().FirstOrDefault();
		List<string> list2 = ((text2 != null && Directory.Exists(GameInstall.ModsDirectory(text2))) ? (from m in Directory.EnumerateDirectories(GameInstall.ModsDirectory(text2)).Select(Path.GetFileName).OfType<string>()
			where !m.StartsWith("BlueprintLoader", StringComparison.OrdinalIgnoreCase)
			select m).OrderBy<string, string>((string m) => m, StringComparer.OrdinalIgnoreCase).ToList() : new List<string>());
		Console.WriteLine();
		Console.WriteLine("Mods");
		if (list2.Count == 0)
		{
			Console.WriteLine("  No mods found in the game's ~mods folder.");
			return 0;
		}
		List<ModVerdict> list3 = new List<ModVerdict>();
		List<string> list4 = new List<string>();
		foreach (string item2 in list2)
		{
			string text3 = LogCommand.FindSave(item2, "NeoRune_" + item2 + "_Trail.sav", "NeoRuneTrailData");
			if (text3 == null)
			{
				list4.Add(item2);
				continue;
			}
			List<string> lines;
			try
			{
				lines = LogCommand.ReadStringArray(File.ReadAllBytes(text3), "Steps");
			}
			catch (IOException)
			{
				list4.Add(item2);
				continue;
			}
			list3.Add(ModVerdict.For(item2, ModVerdict.Parse(lines), crashReport.TimeUtc));
		}
		foreach (ModVerdict item3 in from v in list3
			orderby (!v.Interrupted) ? (v.Active ? 1 : 2) : 0, v.LastSecondsBefore ?? double.MaxValue
			select v)
		{
			if (item3.Interrupted)
			{
				Console.WriteLine($"  [LIKELY] {item3.Mod}: the crash came {Seconds(item3.LastSecondsBefore.Value)} into \"{item3.Unfinished}\", which never finished.");
			}
			else if (item3.Active)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(47, 3);
				defaultInterpolatedStringHandler.AppendLiteral("  [active] ");
				defaultInterpolatedStringHandler.AppendFormatted(item3.Mod);
				defaultInterpolatedStringHandler.AppendLiteral(": was running (");
				List<TrailStep> steps = item3.Steps;
				defaultInterpolatedStringHandler.AppendFormatted(Describe(steps[steps.Count - 1]));
				defaultInterpolatedStringHandler.AppendLiteral(", ");
				defaultInterpolatedStringHandler.AppendFormatted(Seconds(item3.LastSecondsBefore.Value));
				defaultInterpolatedStringHandler.AppendLiteral(" before the crash).");
				Console.WriteLine(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			else if (item3.Steps.Count > 0)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(77, 4);
				defaultInterpolatedStringHandler2.AppendLiteral("  [quiet]  ");
				defaultInterpolatedStringHandler2.AppendFormatted(item3.Mod);
				defaultInterpolatedStringHandler2.AppendLiteral(": nothing in the last ");
				defaultInterpolatedStringHandler2.AppendFormatted(5.0, "0");
				defaultInterpolatedStringHandler2.AppendLiteral(" minutes before the crash (last: ");
				List<TrailStep> steps2 = item3.Steps;
				defaultInterpolatedStringHandler2.AppendFormatted(Describe(steps2[steps2.Count - 1]));
				defaultInterpolatedStringHandler2.AppendLiteral(", ");
				defaultInterpolatedStringHandler2.AppendFormatted(Ago(TimeSpan.FromSeconds(item3.LastSecondsBefore.GetValueOrDefault())));
				defaultInterpolatedStringHandler2.AppendLiteral(" before).");
				Console.WriteLine(defaultInterpolatedStringHandler2.ToStringAndClear());
			}
			else
			{
				Console.WriteLine("  [quiet]  " + item3.Mod + ": no breadcrumbs from before the crash.");
			}
			if (!item3.Interrupted && !item3.Active)
			{
				continue;
			}
			foreach (TrailStep item4 in item3.Steps.TakeLast(5))
			{
				Console.WriteLine($"             {item4.TimeUtc.ToLocalTime():HH:mm:ss}  {item4.Text}");
			}
		}
		if (list4.Count > 0)
		{
			Console.WriteLine("  No breadcrumbs (not NeoRune mods, or built with NeoRune before 1.0): " + string.Join(", ", list4));
		}
		Console.WriteLine();
		if (!flag)
		{
			Console.WriteLine("Verdict: not caused by a mod.");
		}
		else
		{
			ModVerdict modVerdict = list3.FirstOrDefault((ModVerdict v) => v.Interrupted);
			if ((object)modVerdict != null)
			{
				Console.WriteLine($"Verdict: most likely {modVerdict.Mod} (\"{modVerdict.Unfinished}\"). To confirm, remove it from ~mods and do the same thing again.");
			}
			else if (list3.Any((ModVerdict v) => v.Active))
			{
				Console.WriteLine("Verdict: no mod was in the middle of a risky step. The active mods above are the first suspects; remove them one at a time to find out.");
			}
			else if (list3.Count == 0)
			{
				Console.WriteLine("Verdict: unknown. None of the installed mods leaves breadcrumbs yet (mods built with NeoRune 1.0 do). Remove mods one at a time to find out.");
			}
			else
			{
				Console.WriteLine("Verdict: no NeoRune mod was doing anything right before the crash. If it happens again, remove mods one at a time to find out.");
			}
		}
		return 0;
	}

	private static string FirstLine(string text)
	{
		string text2 = (from l in text.Split('\n')
			select l.Trim()).FirstOrDefault((string l) => l.Length > 0) ?? "";
		if (text2.Length <= 110)
		{
			return text2;
		}
		return text2.Substring(0, 107) + "...";
	}

	private static string Describe(TrailStep s)
	{
		if (!s.Text.StartsWith("start "))
		{
			return "\"" + s.Text + "\"";
		}
		string text = s.Text;
		return "started in " + text.Substring(6, text.Length - 6);
	}

	private static string Seconds(double s)
	{
		if (!(s < 1.0))
		{
			if (s < 120.0)
			{
				return $"{s:0} s";
			}
			return $"{s / 60.0:0} min";
		}
		return "under a second";
	}

	private static string Ago(TimeSpan t)
	{
		if (!(t.TotalMinutes < 1.0))
		{
			if (!(t.TotalHours < 1.0))
			{
				if (t.TotalDays < 1.0)
				{
					return $"{t.TotalHours:0.#} h";
				}
				return $"{t.TotalDays:0.#} days";
			}
			return $"{t.TotalMinutes:0} min";
		}
		return $"{t.TotalSeconds:0} s";
	}
}
