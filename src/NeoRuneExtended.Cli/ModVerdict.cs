using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoRuneExtended.Cli;

internal sealed record ModVerdict(string Mod, List<TrailStep> Steps, string? Unfinished, double? LastSecondsBefore)
{
	public bool Interrupted
	{
		get
		{
			if (Unfinished != null)
			{
				double? lastSecondsBefore = LastSecondsBefore;
				if (lastSecondsBefore.HasValue)
				{
					double valueOrDefault = lastSecondsBefore.GetValueOrDefault();
					return valueOrDefault <= 120.0;
				}
			}
			return false;
		}
	}

	public bool Active
	{
		get
		{
			double? lastSecondsBefore = LastSecondsBefore;
			if (lastSecondsBefore.HasValue)
			{
				double valueOrDefault = lastSecondsBefore.GetValueOrDefault();
				return valueOrDefault <= 300.0;
			}
			return false;
		}
	}

	public const double InterruptedWindow = 120.0;

	public const double ActiveWindow = 300.0;

	public static List<TrailStep> Parse(IEnumerable<string> lines)
	{
		List<TrailStep> list = new List<TrailStep>();
		foreach (string line in lines)
		{
			int num = line.IndexOf(' ');
			if (num > 0 && long.TryParse(line.Substring(0, num), out var result))
			{
				DateTime utcDateTime = DateTimeOffset.FromUnixTimeSeconds(result).UtcDateTime;
				string text = line;
				int num2 = num + 1;
				list.Add(new TrailStep(utcDateTime, text.Substring(num2, text.Length - num2)));
			}
		}
		return list;
	}

	public static ModVerdict For(string mod, List<TrailStep> steps, DateTime crashUtc)
	{
		List<TrailStep> list = steps.Where((TrailStep s) => s.TimeUtc <= crashUtc.AddSeconds(2.0)).ToList();
		string unfinished = null;
		List<string> list2 = new List<string>();
		foreach (TrailStep item in list)
		{
			if (item.Text.StartsWith("begin "))
			{
				string text = item.Text;
				list2.Add(text.Substring(6, text.Length - 6));
			}
			else if (item.Text.StartsWith("end "))
			{
				string text = item.Text;
				list2.Remove(text.Substring(4, text.Length - 4));
			}
			else if (item.Text.StartsWith("start "))
			{
				list2.Clear();
			}
		}
		if (list2.Count > 0)
		{
			unfinished = list2[list2.Count - 1];
		}
		double? num;
		if (list.Count <= 0)
		{
			num = null;
		}
		else
		{
			num = Math.Max(0.0, (crashUtc - list[list.Count - 1].TimeUtc).TotalSeconds);
		}
		double? lastSecondsBefore = num;
		return new ModVerdict(mod, list, unfinished, lastSecondsBefore);
	}
}
