using System;

namespace NeoRune;

/// <summary>Binds a C# enum to an Unreal enum.</summary>
[AttributeUsage(AttributeTargets.Enum, Inherited = false)]
public sealed class UEnumAttribute : Attribute
{
	public string Path { get; }

	public UEnumAttribute(string path)
	{
		Path = path;
	}
}
