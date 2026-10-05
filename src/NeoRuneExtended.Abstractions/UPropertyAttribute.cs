using System;

namespace NeoRune;

/// <summary>
/// Binds a field or property to a UProperty of the given type descriptor. <paramref name="owner" /> is the struct that
/// declares the property when it differs from the containing C# struct (fields inherited from a super struct are
/// flattened into the derived struct, since C# structs can't inherit).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event, Inherited = false)]
public sealed class UPropertyAttribute : Attribute
{
	public string Name { get; }

	public string Type { get; }

	public string? Owner { get; }

	public UPropertyAttribute(string name, string type, string? owner = null)
	{
		Name = name;
		Type = type;
		Owner = owner;
	}
}
