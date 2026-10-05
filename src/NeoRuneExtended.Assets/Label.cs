namespace NeoRuneExtended.Assets;

public sealed class Label
{
	internal uint? Offset;

	public string Name { get; }

	internal Label(string name)
	{
		Name = name;
	}

	public override string ToString()
	{
		return Name;
	}
}
