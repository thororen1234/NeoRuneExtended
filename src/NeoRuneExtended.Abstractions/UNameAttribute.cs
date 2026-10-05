using System;

namespace NeoRune;

/// <summary>Overrides the mod-visible name of a variable or function in the generated Blueprint.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field)]
public sealed class UNameAttribute : Attribute
{
	public string Name { get; }

	public UNameAttribute(string name)
	{
		Name = name;
	}
}
