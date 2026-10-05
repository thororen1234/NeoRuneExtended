using NeoRuneExtended.Assets;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.UnrealTypes;

namespace NeoRuneExtended.Compiler;

internal sealed record SelfMember(string Name, UType Type, FPackageIndex Owner) : Var(Type)
{
	public override KismetExpression Expr(ScriptBuilder s)
	{
		return s.Member(Name, Owner);
	}

	public override KismetPropertyPointer Pointer(ScriptBuilder s)
	{
		return s.Pointer(Name, s.Ref(Owner));
	}
}
