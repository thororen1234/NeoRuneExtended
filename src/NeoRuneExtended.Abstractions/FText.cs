using System.Runtime.InteropServices;

namespace NeoRune;

/// <summary>An Unreal FText (localized display text). Converts implicitly from string.</summary>
[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct FText
{
	[Intrinsic("text.from-string")]
	public static implicit operator FText(string value)
	{
		throw FName.Stub();
	}

	[Intrinsic("text.to-string")]
	public override string ToString()
	{
		throw FName.Stub();
	}
}
