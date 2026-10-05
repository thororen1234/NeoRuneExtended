using System;
using NeoRuneExtended.Assets;

namespace NeoRuneExtended.Cli;

internal static class SeedCommand
{
	public static int Run(Args a)
	{
		string directory = a.Require("out");
		string text = new BlueprintBuilder("/Game/NeoRune/Seed", "Seed", "/Script/Engine.SaveGame").Write(directory);
		Console.WriteLine("Wrote " + text + "; copy Seed.uasset/.uexp to src/NeoRuneExtended.Assets/Seed/seed.uasset/.uexp");
		return 0;
	}
}
