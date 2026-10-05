using NeoRuneExtended.Assets;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.UnrealTypes;

namespace NeoRuneExtended.Compiler;

internal sealed record StructMember(Var Struct, string Name, UType Type, FPackageIndex StructIndex) : Var(Type)
{
	public override KismetExpression Expr(ScriptBuilder s)
	{
		return new EX_StructMemberContext
		{
			StructMemberExpression = Pointer(s),
			StructExpression = Struct.Expr(s)
		};
	}

	public override KismetPropertyPointer Pointer(ScriptBuilder s)
	{
		return s.Pointer(Name, s.Ref(StructIndex));
	}
}
