using System;
using System.Collections.Generic;
using System.Linq;
using NeoRuneExtended.Assets;

namespace NeoRuneExtended.Compiler;

public sealed record UFunctionInfo(string OwnerPath, string Name, uint Flags, IReadOnlyList<UParam> Params, bool ScriptOnly)
{
	public string Path => OwnerPath + ":" + Name;

	public bool IsStatic => (Flags & 0x2000) != 0;

	public bool IsFinal => (Flags & 1) != 0;

	public bool IsNative => (Flags & 0x400) != 0;

	public UParam? Return => Params.FirstOrDefault((UParam p) => p.IsReturn);

	public IEnumerable<UParam> Inputs => Params.Where((UParam p) => !p.IsReturn);

	public bool HasWildcards
	{
		get
		{
			switch (OwnerPath)
			{
			case "/Script/Engine.KismetArrayLibrary":
			case "/Script/Engine.BlueprintMapLibrary":
			case "/Script/Engine.BlueprintSetLibrary":
			case "/Script/Engine.DataTableFunctionLibrary":
				return true;
			default:
				return false;
			}
		}
	}

	public bool IsMathCall
	{
		get
		{
			if (IsFinal && IsStatic && IsNative && (Flags & 0x12051CC) == 0)
			{
				return !HasWildcards;
			}
			return false;
		}
	}

	public const uint FUNC_Final = 1u;

	public const uint FUNC_BlueprintAuthorityOnly = 4u;

	public const uint FUNC_BlueprintCosmetic = 8u;

	public const uint FUNC_Net = 64u;

	public const uint FUNC_NetReliable = 128u;

	public const uint FUNC_NetRequest = 256u;

	public const uint FUNC_Native = 1024u;

	public const uint FUNC_Event = 2048u;

	public const uint FUNC_NetResponse = 4096u;

	public const uint FUNC_Static = 8192u;

	public const uint FUNC_NetMulticast = 16384u;

	public const uint FUNC_Public = 131072u;

	public const uint FUNC_Private = 262144u;

	public const uint FUNC_Protected = 524288u;

	public const uint FUNC_Delegate = 1048576u;

	public const uint FUNC_NetServer = 2097152u;

	public const uint FUNC_HasOutParms = 4194304u;

	public const uint FUNC_NetClient = 16777216u;

	public const uint FUNC_BlueprintCallable = 67108864u;

	public const uint FUNC_BlueprintEvent = 134217728u;

	public const uint FUNC_BlueprintPure = 268435456u;

	public const uint FUNC_Const = 1073741824u;

	private const uint NetFuncFlags = 18890944u;

	public static IReadOnlyList<UParam> ParseSignature(string signature)
	{
		if (signature.Length != 0)
		{
			return signature.Split(';').Select(delegate(string part)
			{
				int num = part.IndexOf(':');
				int num2 = part.LastIndexOf(':');
				string name = part.Substring(0, num);
				int num3 = num + 1;
				UType type = UType.Parse(part.Substring(num3, num2 - num3));
				num3 = num2 + 1;
				return new UParam(name, type, Convert.ToUInt64(part.Substring(num3, part.Length - num3), 16));
			}).ToList();
		}
		return Array.Empty<UParam>();
	}
}
