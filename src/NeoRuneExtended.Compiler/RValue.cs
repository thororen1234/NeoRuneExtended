using NeoRuneExtended.Assets;
using UAssetAPI.Kismet.Bytecode;

namespace NeoRuneExtended.Compiler;

internal sealed record RValue(KismetExpression Expr, UType Type) : Value(Type);
