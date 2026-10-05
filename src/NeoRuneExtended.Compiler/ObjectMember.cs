using NeoRuneExtended.Assets;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.UnrealTypes;

namespace NeoRuneExtended.Compiler;

internal sealed record ObjectMember(KismetExpression Object, string Name, UType Type, FPackageIndex Owner) : Var(Type)
{
	public override KismetExpression Expr(ScriptBuilder s)
	{
		return s.Context(Object, s.Member(Name, Owner), Pointer(s));
	}

	public override KismetPropertyPointer Pointer(ScriptBuilder s)
	{
		return s.Pointer(Name, s.Ref(Owner));
	}
}
