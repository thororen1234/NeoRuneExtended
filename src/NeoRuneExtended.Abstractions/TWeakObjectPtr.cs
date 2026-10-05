using System.Runtime.InteropServices;

namespace NeoRune;

/// <summary>A weak object reference (does not keep the object alive).</summary>
[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct TWeakObjectPtr<T> where T : class
{
	[Intrinsic("weak.get")]
	public T? Get()
	{
		throw FName.Stub();
	}

	[Intrinsic("weak.from")]
	public static implicit operator TWeakObjectPtr<T>(T? value)
	{
		throw FName.Stub();
	}
}
