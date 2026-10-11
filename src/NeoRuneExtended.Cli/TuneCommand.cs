using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NeoRuneExtended.Cli;

internal static class TuneCommand
{
	private const int Port = 47321;

	private static string FlagPath(string mod)
	{
		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Dungeons2", "Saved", "NeoRune", "tune-" + mod + ".on");
	}

	public static int Run(Args a)
	{
		if (a.Positional.Count < 1)
		{
			throw new UsageException("tune needs <ModName>");
		}
		string mod = a.Positional[0];
		TuneSession session = new TuneSession();
		foreach (string item in a.Positional.Skip(1))
		{
			Console.WriteLine(session.Apply(item) ?? ("ignored \"" + item + "\" (write key=value)"));
		}
		TcpListener listener;
		try
		{
			listener = new TcpListener(IPAddress.Loopback, 47321);
			listener.Start();
		}
		catch (SocketException)
		{
			throw new InvalidOperationException($"port {47321} is in use: is another \"neorunex tune\" running? Close it first.");
		}
		string flag = FlagPath(mod);
		Directory.CreateDirectory(Path.GetDirectoryName(flag));
		File.WriteAllText(flag, "neorunex tune");
		Console.CancelKeyPress += delegate
		{
			Cleanup();
		};
		session.DefaultsReported += delegate(IReadOnlyDictionary<string, string> reported)
		{
			Console.WriteLine("\n" + mod + " can be tuned: " + string.Join(", ", reported.Select<KeyValuePair<string, string>, string>((KeyValuePair<string, string> p) => p.Key + " (default " + p.Value + ")")) + "\n> ");
		};
		Task.Run(() => Serve(listener, mod, session));
		Console.WriteLine("Tuning " + mod + " (Debug builds only). Type key=value to change a value, key= to go back to the default, \"list\" to see them, \"quit\" to stop.");
		Console.WriteLine("Waiting for the mod: start the game (or it's already running) and get to where the mod runs.");
		try
		{
			while (true)
			{
				Console.Write("> ");
				string text = Console.ReadLine();
				bool flag2 = text == null;
				if (!flag2)
				{
					string text2 = text.Trim();
					bool flag3 = ((text2 == "quit" || text2 == "exit") ? true : false);
					flag2 = flag3;
				}
				if (flag2)
				{
					break;
				}
				text = text.Trim();
				if (text.Length == 0)
				{
					continue;
				}
				if (text == "list")
				{
					List<(string Key, string Default, string? Value)> list = session.Table();
					if (list.Count == 0)
					{
						Console.WriteLine("  The mod hasn't reported its values yet (it does within a second or two once it runs and asks for one).");
					}
					foreach (var (value, text3, text4) in list)
					{
						Console.WriteLine($"  {value} = {text4 ?? text3}{((text4 != null) ? ("   (default " + text3 + ")") : "")}");
					}
				}
				else
				{
					Console.WriteLine(session.Apply(text) ?? "write key=value (or \"list\", \"quit\")");
				}
			}
		}
		finally
		{
			Cleanup();
		}
		return 0;
		void Cleanup()
		{
			try
			{
				File.Delete(flag);
			}
			catch (IOException)
			{
			}
			listener.Stop();
		}
	}

	internal static async Task Serve(TcpListener listener, string mod, TuneSession session)
	{
		while (true)
		{
			TcpClient client;
			try
			{
				client = await listener.AcceptTcpClientAsync();
			}
			catch (Exception ex) when (((ex is ObjectDisposedException || ex is SocketException || ex is InvalidOperationException) ? 1 : 0) != 0)
			{
				break;
			}
			_ = Task.Run(() => Handle(client, mod, session));
		}
	}

	private static async Task Handle(TcpClient client, string mod, TuneSession session)
	{
		using (client)
		{
			int num = 2;
			try
			{
				NetworkStream stream = client.GetStream();
				(string, string, string) obj = await ReadRequest(stream);
				string item = obj.Item1;
				string item2 = obj.Item2;
				string item3 = obj.Item3;
				string value = "200 OK";
				string s;
				if (item == "GET" && item2.Equals("/" + mod, StringComparison.OrdinalIgnoreCase))
				{
					s = session.Body();
				}
				else if (item == "POST" && item2.Equals("/" + mod + "/defaults", StringComparison.OrdinalIgnoreCase))
				{
					session.ReportDefaults(item3);
					s = "ok";
				}
				else
				{
					value = "404 Not Found";
					s = "not tuning that mod";
				}
				byte[] bytes = Encoding.UTF8.GetBytes(s);
				byte[] bytes2 = Encoding.ASCII.GetBytes($"HTTP/1.1 {value}\r\nContent-Type: text/plain; charset=utf-8\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n");
				await stream.WriteAsync(bytes2);
				await stream.WriteAsync(bytes);
			}
			catch (Exception ex) when (((ex is IOException || ex is SocketException || ex is FormatException) ? 1 : 0) != 0)
			{
			}
		}
	}

	internal static async Task<(string Method, string Path, string Body)> ReadRequest(Stream stream)
	{
		List<byte> buffer = new List<byte>();
		byte[] chunk = new byte[4096];
		int headerEnd;
		for (headerEnd = -1; headerEnd < 0; headerEnd = IndexOf(buffer, "\r\n\r\n"u8.ToArray()))
		{
			int num = await stream.ReadAsync(chunk);
			if (num == 0)
			{
				throw new IOException("connection closed");
			}
			buffer.AddRange(chunk.AsSpan(0, num).ToArray());
			if (buffer.Count > 65536)
			{
				throw new FormatException("request too large");
			}
		}
		string[] array = Encoding.ASCII.GetString(buffer.GetRange(0, headerEnd).ToArray()).Split("\r\n");
		string[] parts = array[0].Split(' ');
		if (parts.Length < 2)
		{
			throw new FormatException("bad request line");
		}
		int length = (from h in array.Skip(1)
			select h.Split(':', 2) into h
			where h.Length == 2 && h[0].Trim().Equals("Content-Length", StringComparison.OrdinalIgnoreCase)
			select int.TryParse(h[1].Trim(), out var result) ? result : 0).FirstOrDefault();
		if (length > 65536)
		{
			throw new FormatException("body too large");
		}
		while (buffer.Count < headerEnd + 4 + length)
		{
			int num2 = await stream.ReadAsync(chunk);
			if (num2 == 0)
			{
				break;
			}
			buffer.AddRange(chunk.AsSpan(0, num2).ToArray());
		}
		string item = Encoding.UTF8.GetString(buffer.Skip(headerEnd + 4).Take(length).ToArray());
		return (Method: parts[0], Path: Uri.UnescapeDataString(parts[1]), Body: item);
	}

	private static int IndexOf(List<byte> data, byte[] pattern)
	{
		for (int i = 0; i + pattern.Length <= data.Count; i++)
		{
			int j;
			for (j = 0; j < pattern.Length && data[i + j] == pattern[j]; j++)
			{
			}
			if (j == pattern.Length)
			{
				return i;
			}
		}
		return -1;
	}
}
