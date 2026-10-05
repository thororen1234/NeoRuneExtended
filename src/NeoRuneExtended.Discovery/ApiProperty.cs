namespace NeoRuneExtended.Discovery;

public sealed class ApiProperty
{
	public string Name { get; set; } = "";

	public TypeRef Type { get; set; } = new TypeRef();

	public int Offset { get; set; }

	public ulong Flags { get; set; }
}
