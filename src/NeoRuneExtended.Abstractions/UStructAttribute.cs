using System;

namespace NeoRune;

/// <summary>Binds a C# struct to an Unreal script struct.</summary>
[AttributeUsage(AttributeTargets.Struct, Inherited = false)]
public sealed class UStructAttribute : Attribute
{
	public string Path { get; }

	public int Size { get; }

	public UStructAttribute(string path, int size)
	{
		Path = path;
		Size = size;
	}
}
