using NeoRuneExtended.Assets;

namespace NeoRuneExtended.Compiler;

public sealed record UParam(string Name, UType Type, ulong Flags)
{
	public bool IsReturn => (Flags & 0x400) != 0;

	public bool IsOut
	{
		get
		{
			if (!IsReturn && (Flags & 0x100) != 0L)
			{
				return (Flags & 2) == 0;
			}
			return false;
		}
	}

	public bool IsRef
	{
		get
		{
			if (IsOut)
			{
				return (Flags & 0x8000000) != 0;
			}
			return false;
		}
	}

	public const ulong CPF_ConstParm = 2uL;

	public const ulong CPF_Parm = 128uL;

	public const ulong CPF_OutParm = 256uL;

	public const ulong CPF_ReturnParm = 1024uL;

	public const ulong CPF_ReferenceParm = 134217728uL;
}
