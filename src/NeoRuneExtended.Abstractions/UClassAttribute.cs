using System;

namespace NeoRune;

/// <summary>Binds a C# class to an Unreal class (native, AngelScript or Blueprint).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
public sealed class UClassAttribute : Attribute
{
	public string Path { get; }

	public UClassAttribute(string path)
	{
		Path = path;
	}
}
