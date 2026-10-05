namespace NeoRuneExtended.Discovery;

public sealed class TypeRef
{
	public string Kind { get; set; } = "unknown";

	public string? Path { get; set; }

	public string? Meta { get; set; }

	public TypeRef? Inner { get; set; }

	public TypeRef? Value { get; set; }

	public string? Raw { get; set; }

	public override string ToString()
	{
		switch (Kind)
		{
		case "array":
			return $"Array<{Inner}>";
		case "set":
			return $"Set<{Inner}>";
		case "map":
			return $"Map<{Inner},{Value}>";
		default:
			if (Path != null)
			{
				return $"{Kind}<{Path}{((Meta != null) ? ("," + Meta) : "")}>";
			}
			return Kind;
		}
	}
}
