using System;

namespace NeoRune;

/// <summary>A failed <see cref="T:NeoRune.ModAssert" /> check.</summary>
public sealed class ModAssertException : Exception
{
	public ModAssertException(string message)
		: base(message)
	{
	}
}
