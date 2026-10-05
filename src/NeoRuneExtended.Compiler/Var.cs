using NeoRuneExtended.Assets;
using UAssetAPI.Kismet.Bytecode;

namespace NeoRuneExtended.Compiler;

internal abstract record Var(UType Type) : Value(Type)
{
	public abstract KismetExpression Expr(ScriptBuilder s);

	public abstract KismetPropertyPointer Pointer(ScriptBuilder s);
}
