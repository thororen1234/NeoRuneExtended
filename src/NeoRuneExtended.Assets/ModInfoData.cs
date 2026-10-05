using System;
using System.Collections.Generic;

namespace NeoRuneExtended.Assets;

public sealed class ModInfoData
{
	public required string ModName { get; init; }

	public string? Version { get; init; }

	public string? Author { get; init; }

	public string? AuthorUrl { get; init; }

	public string? Description { get; init; }

	public IReadOnlyList<ModSettingData> Settings { get; init; } = Array.Empty<ModSettingData>();
}
