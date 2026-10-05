using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using NeoRuneExtended.Assets;
using UAssetAPI.UnrealTypes;

namespace NeoRuneExtended.Compiler;

internal sealed class MethodPlan
{
	public required IMethodSymbol Method { get; init; }

	public required string Name { get; init; }

	public FunctionBuilder Builder { get; set; }

	public FPackageIndex? Index => Builder?.Index;

	public bool External { get; init; }

	public bool ByName { get; init; }

	/// <summary>
	/// A static function of a Blueprint function library: it has the hidden __WorldContext parameter, which Params leaves
	/// out, so it's only for callers outside the mod.
	/// </summary>
	public bool Library { get; init; }

	public List<(ISymbol Symbol, string Name, UType Type, bool IsOut)> Params { get; } = new List<(ISymbol, string, UType, bool)>();

	public string? ReturnName { get; set; }

	public UType? ReturnType { get; set; }

	public bool Compiled { get; set; }
}
