namespace NeoRuneExtended.Cli;

internal sealed record PerfRow(string Name, long Calls, double Seconds)
{
	public double MicrosPerCall
	{
		get
		{
			if (Calls <= 0)
			{
				return 0.0;
			}
			return Seconds * 1000000.0 / (double)Calls;
		}
	}

	public double MsPerFrame(long frames)
	{
		if (frames <= 0)
		{
			return 0.0;
		}
		return Seconds * 1000.0 / (double)frames;
	}
}
