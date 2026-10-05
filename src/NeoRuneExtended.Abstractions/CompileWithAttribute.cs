using System;

namespace NeoRune;

/// <summary>
/// SDK use: when the marked class is compiled into a mod, also compile the named class of the same namespace. For a
/// class that's only loaded by path at run time (and so never referenced), e.g. code that depends on a game plugin.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class CompileWithAttribute : Attribute
{
	public string ClassName { get; }

	public CompileWithAttribute(string className)
	{
		ClassName = className;
	}
}
