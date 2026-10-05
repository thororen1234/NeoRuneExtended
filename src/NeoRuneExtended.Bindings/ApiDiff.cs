using System.Collections.Generic;
using System.Linq;
using NeoRuneExtended.Discovery;

namespace NeoRuneExtended.Bindings;

public static class ApiDiff
{
	public static List<string> Compare(ApiDump oldDump, ApiDump newDump)
	{
		List<string> list = new List<string>();
		Dictionary<string, ApiType> dictionary = (from t in newDump.Types
			group t by t.Path).ToDictionary((IGrouping<string, ApiType> g) => g.Key, (IGrouping<string, ApiType> g) => g.First());
		foreach (ApiType type in oldDump.Types)
		{
			if (type.Path.StartsWith("/Game/"))
			{
				continue;
			}
			if (!dictionary.TryGetValue(type.Path, out var value))
			{
				list.Add("removed " + type.Kind + " " + type.Path);
				continue;
			}
			if (type.Super != value.Super)
			{
				list.Add($"changed {type.Path}: parent {type.Super} -> {value.Super}");
			}
			Dictionary<string, ApiProperty> dictionary2 = (value.Properties ?? new List<ApiProperty>()).ToDictionary((ApiProperty p) => p.Name, (ApiProperty p) => p);
			foreach (ApiProperty item in type.Properties ?? new List<ApiProperty>())
			{
				if (!dictionary2.TryGetValue(item.Name, out var value2))
				{
					list.Add("removed property " + type.Path + "." + item.Name);
				}
				else if (value2.Type.ToString() != item.Type.ToString())
				{
					list.Add($"changed property {type.Path}.{item.Name}: {item.Type} -> {value2.Type}");
				}
			}
			Dictionary<string, ApiFunction> dictionary3 = (from f in value.Functions ?? new List<ApiFunction>()
				group f by f.Name).ToDictionary((IGrouping<string, ApiFunction> g) => g.Key, (IGrouping<string, ApiFunction> g) => g.First());
			foreach (ApiFunction item2 in type.Functions ?? new List<ApiFunction>())
			{
				if (!dictionary3.TryGetValue(item2.Name, out var value3))
				{
					list.Add("removed function " + type.Path + ":" + item2.Name);
					continue;
				}
				string text = Signature(item2);
				string text2 = Signature(value3);
				if (text != text2)
				{
					list.Add($"changed function {type.Path}:{item2.Name}: ({text}) -> ({text2})");
				}
				if ((item2.Flags & 0x8002401) != (value3.Flags & 0x8002401))
				{
					list.Add($"changed function {type.Path}:{item2.Name}: flags 0x{item2.Flags:x} -> 0x{value3.Flags:x}");
				}
			}
			if (!(type.Kind == "enum"))
			{
				continue;
			}
			Dictionary<string, long> dictionary4 = (value.Values ?? new List<ApiEnumValue>()).ToDictionary((ApiEnumValue v) => v.Name, (ApiEnumValue v) => v.Value);
			foreach (ApiEnumValue item3 in type.Values ?? new List<ApiEnumValue>())
			{
				if (!dictionary4.TryGetValue(item3.Name, out var value4))
				{
					list.Add("removed enum value " + item3.Name);
				}
				else if (value4 != item3.Value)
				{
					list.Add($"changed enum value {item3.Name}: {item3.Value} -> {value4}");
				}
			}
		}
		return list;
	}

	private static string Signature(ApiFunction f)
	{
		return string.Join(", ", f.Params.Select((ApiProperty p) => $"{p.Name}:{p.Type}:{p.Flags & 0x8000582:x}"));
	}
}
