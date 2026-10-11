using System;
using System.IO;
using System.Linq;
using NeoRuneExtended.Discovery;
using NeoRuneExtended.Packaging;

namespace NeoRuneExtended.Cli;

internal static class PerfCommand
{
	public static int Run(Args a)
	{
		if (a.Positional.Count != 1)
		{
			throw new UsageException("perf needs <ModName>");
		}
		string text = a.Positional[0];
		string path = Path.Combine(GameInstall.ModsDirectory(ModPackager.ResolveGame(a.Get("game"))), text, text + "_P.neorune.json");
		ModManifest modManifest = (File.Exists(path) ? ModManifest.Read(path) : null);
		if (modManifest?.Profile == null)
		{
			Console.WriteLine("The installed " + text + " isn't a profiling build. In its project folder run:");
			Console.WriteLine("  dotnet build -p:NeoRuneProfile=true");
			Console.WriteLine("then play for a few minutes doing what you want to measure, close the game and run \"neorunex perf " + text + "\" again.");
			Console.WriteLine("(Rebuild normally afterwards: profiling builds are a little slower, and never for releases.)");
			return 1;
		}
		string text2 = LogCommand.FindSave(text, "NeoRune_" + text + "_Perf.sav", "NeoRunePerfData");
		if (text2 == null)
		{
			Console.WriteLine("No measurements yet: start the game and play with " + text + " for a few minutes (they're saved every 5 seconds).");
			return 1;
		}
		PerfReport perfReport = PerfReport.Parse(LogCommand.ReadStringArray(File.ReadAllBytes(text2), "Lines"), modManifest.Profile);
		Console.WriteLine($"{text}: {perfReport.SessionSeconds / 60.0:0.#} min of play, {perfReport.Frames} frames ({((perfReport.SessionSeconds > 0.0) ? ((double)perfReport.Frames / perfReport.SessionSeconds) : 0.0):0} fps), saved {File.GetLastWriteTime(text2):yyyy-MM-dd HH:mm}");
		if (perfReport.Rows.Count == 0)
		{
			Console.WriteLine("None of the mod's functions ran.");
			return 0;
		}
		perfReport.Rows.Sum((PerfRow r) => r.Seconds);
		Console.WriteLine();
		Console.WriteLine($"  {"Function",-44} {"ms/frame",9} {"calls",10} {"us/call",9} {"total ms",10}");
		int num = (a.Flag("all") ? perfReport.Rows.Count : Math.Min(15, perfReport.Rows.Count));
		foreach (PerfRow item in perfReport.Rows.Take(num))
		{
			Console.WriteLine($"  {Trim(item.Name, 44),-44} {item.MsPerFrame(perfReport.Frames),9:0.000} {item.Calls,10} {item.MicrosPerCall,9:0.0} {item.Seconds * 1000.0,10:0.0}");
		}
		if (num < perfReport.Rows.Count)
		{
			Console.WriteLine($"  ... {perfReport.Rows.Count - num} more (--all)");
		}
		Console.WriteLine();
		PerfRow perfRow = perfReport.Rows[0];
		Console.WriteLine($"Most expensive: {perfRow.Name}, {perfRow.MsPerFrame(perfReport.Frames):0.000} ms per frame. Times include what each function calls, so a caller and its callee both count the callee's time.");
		return 0;
	}

	private static string Trim(string s, int max)
	{
		if (s.Length > max)
		{
			return s.Substring(0, max - 1) + "…";
		}
		return s;
	}
}
