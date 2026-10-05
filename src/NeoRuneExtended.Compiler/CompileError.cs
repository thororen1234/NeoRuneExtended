using System;
using Microsoft.CodeAnalysis;

namespace NeoRuneExtended.Compiler;

public sealed class CompileError : Exception
{
	public Location? Location { get; }

	public bool AlreadyReported { get; }

	public CompileError(string message, Location? location)
		: base(message)
	{
		Location = location;
	}

	private CompileError()
		: base("errors reported")
	{
		AlreadyReported = true;
	}

	public static CompileError Reported()
	{
		return new CompileError();
	}
}
