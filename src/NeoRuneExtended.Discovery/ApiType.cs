using System.Collections.Generic;

namespace NeoRuneExtended.Discovery;

public sealed class ApiType
{
	public string Path { get; set; } = "";

	public string Kind { get; set; } = "";

	public string MetaClass { get; set; } = "";

	public string? Super { get; set; }

	public int Size { get; set; }

	public List<ApiProperty>? Properties { get; set; }

	public List<ApiFunction>? Functions { get; set; }

	public List<ApiEnumValue>? Values { get; set; }
}
