using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using NeoRuneExtended.Cli;

[CompilerGenerated]
internal class Program
{
	private static int Main(string[] args)
	{
		List<string> list = Args.Expand(args);
		bool flag = list.Count == 0;
		if (!flag)
		{
			bool flag2;
			switch (list[0])
			{
			case "-h":
			case "--help":
			case "help":
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			flag = flag2;
		}
		if (flag)
		{
			Console.WriteLine(Commands.Help);
			return 0;
		}
		try
		{
			Args a = new Args(list.Skip(1));
			return list[0] switch
			{
				"build" => Commands.Build(a), 
				"install" => Commands.Install(a), 
				"discover" => Commands.Discover(a), 
				"bindings" => Commands.Bindings(a), 
				"diff" => Commands.Diff(a), 
				"disasm" => Commands.Disasm(a), 
				"log" => LogCommand.Run(a), 
				"doctor" => DoctorCommand.Run(a), 
				"inspect" => DevCommands.Inspect(a), 
				"json" => DevCommands.Json(a), 
				"roundtrip" => DevCommands.Roundtrip(a), 
				"phase0" => DevCommands.Phase0(a), 
				"seed" => SeedCommand.Run(a), 
				_ => throw new UsageException("unknown command '" + list[0] + "'"), 
			};
		}
		catch (UsageException ex)
		{
			Console.Error.WriteLine("neorunex: " + ex.Message + "\n\n" + Commands.Help);
			return 2;
		}
		catch (Exception ex2) when (((ex2 is InvalidOperationException || ex2 is FileNotFoundException || ex2 is IOException) ? 1 : 0) != 0)
		{
			Console.Error.WriteLine("neorunex: error: " + ex2.Message);
			return 1;
		}
	}
}
