using NeoRuneExtended.Assets;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.UnrealTypes;

namespace NeoRuneExtended.Compiler;

internal sealed record LocalVar(string Name, UType Type, FPackageIndex Function, bool OutParam = false) : Var(Type)
{
	public override KismetExpression Expr(ScriptBuilder s)
	{
		return s.Local(Name, OutParam);
	}

	public override KismetPropertyPointer Pointer(ScriptBuilder s)
	{
		return s.Pointer(Name, Function);
	}
}
