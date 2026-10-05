using System.Collections.Generic;

namespace NeoRuneExtended.Discovery;

public sealed class ApiFunction
{
	public string Name { get; set; } = "";

	public uint Flags { get; set; }

	public List<ApiProperty> Params { get; set; } = new List<ApiProperty>();
}
