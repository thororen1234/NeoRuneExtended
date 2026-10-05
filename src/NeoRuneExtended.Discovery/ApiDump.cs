using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NeoRuneExtended.Discovery;

public sealed class ApiDump
{
	public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
	{
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public int FormatVersion { get; set; } = 1;

	public GameBuild Game { get; set; } = new GameBuild();

	public List<ApiType> Types { get; set; } = new List<ApiType>();

	public void Save(string path)
	{
		using FileStream utf8Json = File.Create(path);
		JsonSerializer.Serialize(utf8Json, this, Options);
	}

	public static ApiDump Load(string path)
	{
		using FileStream utf8Json = File.OpenRead(path);
		return JsonSerializer.Deserialize<ApiDump>(utf8Json, Options);
	}
}
