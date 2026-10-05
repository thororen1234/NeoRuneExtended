using System;

namespace NeoRune;

/// <summary>
/// Marks a game function as unsafe to call from mods: AngelScript-implemented (non-native)
/// functions crash the game when called from Blueprint bytecode.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class ScriptOnlyAttribute : Attribute
{
}
