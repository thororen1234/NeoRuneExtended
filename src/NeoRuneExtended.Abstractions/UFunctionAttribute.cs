using System;

namespace NeoRune;

/// <summary>
/// Binds a method to a UFunction. Signature lists the parameters in declaration order as
/// "name:descriptor:flags" separated by ';' (flags = hex EPropertyFlags); see NeoRuneExtended.Assets.UType.Parse.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class UFunctionAttribute : Attribute
{
	public string Name { get; }

	public uint Flags { get; }

	public string Signature { get; }

	public UFunctionAttribute(string name, uint flags, string signature)
	{
		Name = name;
		Flags = flags;
		Signature = signature;
	}
}
