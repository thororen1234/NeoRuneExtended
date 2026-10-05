using System;

namespace NeoRuneExtended.Cli;

internal sealed class UsageException : Exception
{
	public UsageException(string message)
		: base(message)
	{
	}
}
