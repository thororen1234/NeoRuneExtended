using System;
using System.Collections.Generic;

namespace NeoRuneExtended.Assets;

public sealed class ModSettingData
{
	public ModSettingType Type { get; init; }

	public string? Id { get; init; }

	public string? Label { get; init; }

	public string? Description { get; init; }

	public bool DefaultOn { get; init; }

	public double DefaultValue { get; init; }

	public double Min { get; init; }

	public double Max { get; init; } = 1.0;

	public double Step { get; init; }

	public double ValueWidth { get; init; }

	public bool Percentage { get; init; }

	public IReadOnlyList<string> Options { get; init; } = Array.Empty<string>();

	public int DefaultOption { get; init; }

	public string? Url { get; init; }

	public string? ButtonText { get; init; }

	public double Height { get; init; } = 16.0;

	public string? DefaultKey { get; init; }

	public string? SecondaryKey { get; init; }

	public string? DefaultText { get; init; }

	public string? Placeholder { get; init; }

	public (float R, float G, float B, float A)? DefaultColour { get; init; }

	public bool HexInput { get; init; }

	public string? WidgetClass { get; init; }
}
