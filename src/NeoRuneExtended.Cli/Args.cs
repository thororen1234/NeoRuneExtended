using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NeoRuneExtended.Cli;

internal sealed class Args
{
	private readonly Dictionary<string, List<string>> options = new Dictionary<string, List<string>>();

	public List<string> Positional { get; } = new List<string>();

	public Args(IEnumerable<string> args)
	{
		string text = null;
		foreach (string arg in args)
		{
			if (arg.StartsWith("--"))
			{
				if (text != null)
				{
					Add(text, "true");
				}
				string text2 = arg;
				text = text2.Substring(2, text2.Length - 2);
				int num = text.IndexOf('=');
				if (num >= 0)
				{
					string name = text.Substring(0, num);
					text2 = text;
					int num2 = num + 1;
					Add(name, text2.Substring(num2, text2.Length - num2));
					text = null;
				}
			}
			else if (text != null)
			{
				Add(text, arg);
				text = null;
			}
			else
			{
				Positional.Add(arg);
			}
		}
		if (text != null)
		{
			Add(text, "true");
		}
	}

	private void Add(string name, string value)
	{
		if (!options.TryGetValue(name, out List<string> value2))
		{
			value2 = (options[name] = new List<string>());
		}
		value2.Add(value);
	}

	public string? Get(string name)
	{
		if (!options.TryGetValue(name, out List<string> value))
		{
			return null;
		}
		List<string> list = value;
		return list[list.Count - 1];
	}

	public string Require(string name)
	{
		return Get(name) ?? throw new UsageException("--" + name + " is required");
	}

	public IReadOnlyList<string> All(string name)
	{
		if (!options.TryGetValue(name, out List<string> value))
		{
			return Array.Empty<string>();
		}
		return value;
	}

	public bool Flag(string name)
	{
		switch (Get(name))
		{
		case "true":
		case "1":
		case "yes":
			return true;
		default:
			return false;
		}
	}

	public static List<string> Expand(IEnumerable<string> args)
	{
		List<string> list = new List<string>();
		foreach (string arg in args)
		{
			if (arg.StartsWith('@'))
			{
				string text = arg;
				if (File.Exists(text.Substring(1, text.Length - 1)))
				{
					text = arg;
					list.AddRange(from l in File.ReadAllLines(text.Substring(1, text.Length - 1))
						select l.Trim() into l
						where l.Length > 0 && !l.StartsWith('#')
						select l.Trim('"'));
					continue;
				}
			}
			list.Add(arg);
		}
		return list;
	}
}
