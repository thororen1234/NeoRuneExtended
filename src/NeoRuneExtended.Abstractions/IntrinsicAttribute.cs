using System;

namespace NeoRune;

/// <summary>
/// Methods marked with this are implemented by the NeoRune compiler directly (an instruction
/// or a fixed call sequence) instead of being compiled from their C# body.
/// </summary>
[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property, Inherited = false)]
public sealed class IntrinsicAttribute : Attribute
{
	public string Id { get; }

	public IntrinsicAttribute(string id)
	{
		Id = id;
	}
}
