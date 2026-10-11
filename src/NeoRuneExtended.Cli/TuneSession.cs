using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoRuneExtended.Cli;

internal sealed class TuneSession
{
	private readonly object gate = new object();

	private readonly Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.Ordinal);

	private readonly Dictionary<string, string> defaults = new Dictionary<string, string>(StringComparer.Ordinal);

	private int version;

	public event Action<IReadOnlyDictionary<string, string>>? DefaultsReported;

	public string? Apply(string line)
	{
		int num = line.IndexOf('=');
		if (num <= 0)
		{
			return null;
		}
		string text = line.Substring(0, num).Trim();
		int num2 = num + 1;
		string text2 = line.Substring(num2, line.Length - num2).Trim();
		lock (gate)
		{
			if (text2.Length == 0)
			{
				values.Remove(text);
			}
			else
			{
				values[text] = text2;
			}
			version++;
		}
		if (text2.Length != 0)
		{
			return text + " = " + text2;
		}
		return text + ": back to the code's default";
	}

	public string Body()
	{
		lock (gate)
		{
			return string.Concat($"v{version}\n", string.Concat(values.Select<KeyValuePair<string, string>, string>((KeyValuePair<string, string> p) => p.Key + "=" + p.Value + "\n")));
		}
	}

	public void ReportDefaults(string body)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
		string[] array = body.Split('\n');
		foreach (string text in array)
		{
			int num = text.IndexOf('=');
			if (num > 0)
			{
				string key = text.Substring(0, num).Trim();
				string text2 = text;
				int num2 = num + 1;
				dictionary[key] = text2.Substring(num2, text2.Length - num2).Trim();
			}
		}
		bool flag;
		lock (gate)
		{
			flag = dictionary.Any((KeyValuePair<string, string> p) => !defaults.TryGetValue(p.Key, out string value) || value != p.Value);
			foreach (KeyValuePair<string, string> item in dictionary)
			{
				defaults[item.Key] = item.Value;
			}
		}
		if (flag)
		{
			DefaultsReported?.Invoke(dictionary);
		}
	}

	public List<(string Key, string Default, string? Value)> Table()
	{
		lock (gate)
		{
			return (from k in defaults.Keys.Union(values.Keys).OrderBy<string, string>((string k) => k, StringComparer.Ordinal)
				select (k: k, defaults.TryGetValue(k, out string value) ? value : "?", values.TryGetValue(k, out string value2) ? value2 : null)).ToList();
		}
	}
}
