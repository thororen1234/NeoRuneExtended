using System;

namespace NeoRune;

/// <summary>Binds a delegate type to a UFunction delegate signature.</summary>
[AttributeUsage(AttributeTargets.Delegate, Inherited = false)]
public sealed class UDelegateAttribute : Attribute
{
	public string Path { get; }

	public UDelegateAttribute(string path)
	{
		Path = path;
	}
}
