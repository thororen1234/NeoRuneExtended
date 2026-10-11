using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NeoRuneExtended.Cli;

internal sealed record PerfReport(double SessionSeconds, long Frames, List<PerfRow> Rows)
{
	public static PerfReport Parse(IEnumerable<string> lines, IReadOnlyList<string> names)
	{
		double result = 0.0;
		long result2 = 0L;
		List<PerfRow> list = new List<PerfRow>();
		foreach (string line in lines)
		{
			string[] array = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			int result3;
			long result4;
			double result5;
			if (array.Length == 3 && array[0] == "session")
			{
				double.TryParse(array[1], NumberStyles.Float, CultureInfo.InvariantCulture, out result);
				long.TryParse(array[2], out result2);
			}
			else if (array.Length == 3 && int.TryParse(array[0], out result3) && long.TryParse(array[1], out result4) && double.TryParse(array[2], NumberStyles.Float, CultureInfo.InvariantCulture, out result5) && result4 > 0)
			{
				list.Add(new PerfRow((result3 < names.Count) ? names[result3] : $"#{result3}", result4, result5));
			}
		}
		return new PerfReport(result, result2, list.OrderByDescending((PerfRow r) => r.Seconds).ToList());
	}
}
