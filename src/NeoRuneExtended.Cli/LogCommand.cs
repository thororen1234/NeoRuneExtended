using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace NeoRuneExtended.Cli;

internal static class LogCommand
{
	public static string SaveGames => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Dungeons2", "Saved", "SaveGames");

	public static int Run(Args a)
	{
		if (a.Positional.Count != 1)
		{
			throw new UsageException("log needs <ModName> or a .sav path");
		}
		string text = a.Positional[0];
		string text2 = (text.EndsWith(".sav") ? text : Find(text));
		if (text2 == null)
		{
			throw new FileNotFoundException($"No log for {text} yet: neither {Path.Combine(SaveGames, "NeoRune_" + text + ".sav")} nor an Xbox app save exists. " + "The mod has not logged anything, or it has not run (try \"neorune doctor\").");
		}
		if (!text2.EndsWith(".sav"))
		{
			Console.Error.WriteLine("(from Xbox app save storage: " + text2 + ")");
		}
		List<string> list = ReadLines(text2);
		foreach (string item in list)
		{
			Console.WriteLine(item);
		}
		if (a.Flag("follow"))
		{
			Follow(text, text2, list.Count);
		}
		return 0;
	}

	private static void Follow(string arg, string path, int printed)
	{
		Console.Error.WriteLine("(following: Ctrl+C to stop)");
		DateTime dateTime = File.GetLastWriteTimeUtc(path);
		while (true)
		{
			Thread.Sleep(500);
			string text = (arg.EndsWith(".sav") ? path : (Find(arg) ?? path));
			if (!File.Exists(text))
			{
				continue;
			}
			DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(text);
			if (!(text == path) || !(lastWriteTimeUtc == dateTime))
			{
				List<string> list;
				try
				{
					list = ReadLines(text);
				}
				catch (IOException)
				{
					continue;
				}
				if (list.Count < printed)
				{
					Console.WriteLine("--- log cleared ---");
					printed = 0;
				}
				for (int i = printed; i < list.Count; i++)
				{
					Console.WriteLine(list[i]);
				}
				printed = list.Count;
				path = text;
				dateTime = lastWriteTimeUtc;
			}
		}
	}

	private static List<string> ReadLines(string path)
	{
		return ReadStringArray(File.ReadAllBytes(path), "Lines");
	}

	public static string? Find(string mod)
	{
		string text = Path.Combine(SaveGames, "NeoRune_" + mod + ".sav");
		string text2 = FindXboxLog(mod);
		if (!File.Exists(text))
		{
			return text2;
		}
		if (text2 == null)
		{
			return text;
		}
		if (!(File.GetLastWriteTimeUtc(text2) > File.GetLastWriteTimeUtc(text)))
		{
			return text;
		}
		return text2;
	}

	public static IEnumerable<string> XboxSaveFolders()
	{
		string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages");
		if (!Directory.Exists(path))
		{
			return Array.Empty<string>();
		}
		return (from p in Directory.EnumerateDirectories(path)
			where Path.GetFileName(p).Contains("Dungeons2", StringComparison.OrdinalIgnoreCase)
			select Path.Combine(p, "SystemAppData", "wgs")).Where(Directory.Exists);
	}

	private static string? FindXboxLog(string mod)
	{
		byte[] bytes = Encoding.ASCII.GetBytes("/Game/Mods/" + mod + "/NeoRuneLogData");
		string result = null;
		DateTime dateTime = DateTime.MinValue;
		foreach (string item in XboxSaveFolders())
		{
			foreach (string item2 in Directory.EnumerateFiles(item, "*", SearchOption.AllDirectories))
			{
				string fileName = Path.GetFileName(item2);
				if (fileName == "containers.index" || fileName.StartsWith("container."))
				{
					continue;
				}
				FileInfo fileInfo = new FileInfo(item2);
				if (fileInfo.Length > 16777216 || fileInfo.LastWriteTimeUtc <= dateTime)
				{
					continue;
				}
				try
				{
					if (IndexOfIgnoreCase(File.ReadAllBytes(item2), bytes) < 0)
					{
						continue;
					}
				}
				catch (IOException)
				{
					continue;
				}
				result = item2;
				dateTime = fileInfo.LastWriteTimeUtc;
			}
		}
		return result;
	}

	public static List<string> ReadStringArray(byte[] data, string property)
	{
		byte[] bytes = Encoding.ASCII.GetBytes(property + "\0");
		int num = IndexOfIgnoreCase(data, bytes);
		if (num < 0)
		{
			return new List<string>();
		}
		byte[] bytes2 = Encoding.ASCII.GetBytes("StrProperty\0");
		int num2 = IndexOfIgnoreCase(data.AsSpan(num), bytes2);
		if (num2 >= 0 && num2 < 128)
		{
			num += num2 + bytes2.Length - bytes.Length;
		}
		for (int i = num + bytes.Length; i < Math.Min(data.Length - 4, num + 256); i++)
		{
			int num3 = BitConverter.ToInt32(data, i);
			if (num3 > 0 && num3 <= 1000000)
			{
				List<string> list = new List<string>();
				int p = i + 4;
				string s;
				while (list.Count < num3 && TryReadFString(data, ref p, out s))
				{
					list.Add(s);
				}
				if (list.Count == num3)
				{
					return list;
				}
			}
		}
		return new List<string>();
	}

	private static int IndexOfIgnoreCase(ReadOnlySpan<byte> data, byte[] pattern)
	{
		for (int i = 0; i <= data.Length - pattern.Length; i++)
		{
			int j;
			for (j = 0; j < pattern.Length && Lower(data[i + j]) == Lower(pattern[j]); j++)
			{
			}
			if (j == pattern.Length)
			{
				return i;
			}
		}
		return -1;
		static byte Lower(byte b)
		{
			if (b < 65 || b > 90)
			{
				return b;
			}
			return (byte)(b + 32);
		}
	}

	private static bool TryReadFString(byte[] d, ref int p, out string s)
	{
		s = "";
		if (p + 4 > d.Length)
		{
			return false;
		}
		int num = BitConverter.ToInt32(d, p);
		if (num == 0)
		{
			p += 4;
			return true;
		}
		if (num > 0)
		{
			if (num > 100000 || p + 4 + num > d.Length || d[p + 4 + num - 1] != 0)
			{
				return false;
			}
			s = Encoding.Latin1.GetString(d, p + 4, num - 1);
			p += 4 + num;
			return true;
		}
		int num2 = -num;
		if (num2 > 100000 || p + 4 + num2 * 2 > d.Length)
		{
			return false;
		}
		s = Encoding.Unicode.GetString(d, p + 4, (num2 - 1) * 2);
		p += 4 + num2 * 2;
		return true;
	}
}
