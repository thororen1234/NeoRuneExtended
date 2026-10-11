using NeoRuneExtended.Assets;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.UnrealTypes;

namespace NeoRuneExtended.Compiler;

internal sealed record LocalVar(string Name, UType Type, FPackageIndex Function, bool OutParam = false, FPackageIndex? Member = null) : Var(Type)
{
	public override KismetExpression Expr(ScriptBuilder s)
	{
		if (Member == null)
		{
			return s.Local(Name, OutParam);
		}
		return s.Member(Name, Member);
	}

	public override KismetPropertyPointer Pointer(ScriptBuilder s)
	{
		if (Member == null)
		{
			return s.Pointer(Name, Function);
		}
		return s.Pointer(Name, s.Ref(Member));
	}
}
