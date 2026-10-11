using System;

namespace NeoRune;

/// <summary>
/// A class of tests for "neorune test": its public methods without parameters are run as normal .NET code, outside the
/// game, so the mod's logic (formatting, parsing, LINQ, your own structs and helpers) can be checked before playing.
/// It's never part of the mod: the mod compiler and the editor rules leave it alone, so it can use exceptions,
/// <see cref="T:NeoRune.ModAssert" /> and anything else from .NET.
/// <code>
/// [ModTests]
/// public class FormatTests
/// {
///     public void Percent() =&gt; ModAssert.Equal("12%", Stats.Percent(0.12));
/// }
/// </code>
/// Code that calls the game (game classes, Log, World...) can't run there: such a test fails and says so.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ModTestsAttribute : Attribute
{
}
