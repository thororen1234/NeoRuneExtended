using System;
using System.Runtime.InteropServices;

namespace NeoRune;

/// <summary>An Unreal FName. Converts implicitly from string literals.</summary>
[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct FName : IEquatable<FName>
{
	[Intrinsic("name.none")]
	public static FName None
	{
		get
		{
			throw Stub();
		}
	}

	[Intrinsic("name.from-string")]
	public static implicit operator FName(string value)
	{
		throw Stub();
	}

	[Intrinsic("name.to-string")]
	public override string ToString()
	{
		throw Stub();
	}

	[Intrinsic("name.equals")]
	public static bool operator ==(FName a, FName b)
	{
		throw Stub();
	}

	[Intrinsic("name.not-equals")]
	public static bool operator !=(FName a, FName b)
	{
		throw Stub();
	}

	public bool Equals(FName other)
	{
		throw Stub();
	}

	public override bool Equals(object? obj)
	{
		throw Stub();
	}

	public override int GetHashCode()
	{
		throw Stub();
	}

	internal static Exception Stub()
	{
		return new InvalidOperationException("NeoRune mods only run inside the game.");
	}
}
