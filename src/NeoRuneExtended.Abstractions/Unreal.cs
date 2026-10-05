namespace NeoRune;

/// <summary>Compiler built-ins.</summary>
public static class Unreal
{
	/// <summary>The name of the mod being compiled (its folder under Content/Mods).</summary>
	[Intrinsic("mod-name")]
	public static string ModName
	{
		get
		{
			throw FName.Stub();
		}
	}

	/// <summary>
	/// Reads a data table row into a struct of the table's row type (UE's "Get Data Table Row").
	/// Returns false when the row does not exist or T does not match the table.
	/// </summary>
	[Intrinsic("datatable-row")]
	public static bool DataTableRow<T>(object? table, FName rowName, out T row) where T : struct
	{
		throw FName.Stub();
	}

	/// <summary>The Unreal class object of T.</summary>
	[Intrinsic("class-of")]
	public static TSubclassOf<T> ClassOf<T>() where T : class
	{
		throw FName.Stub();
	}

	/// <summary>
	/// The class at a constant asset path, e.g. "/Game/Foo/BP_Bar.BP_Bar_C". It's a hard dependency of the mod's package:
	/// if the class doesn't exist, the mod fails to load. Use <see cref="M:NeoRune.Unreal.LoadClass``1(System.String)" /> for classes that may be missing.
	/// </summary>
	[Intrinsic("class-at")]
	public static TSubclassOf<T> ClassAt<T>(string path) where T : class
	{
		throw FName.Stub();
	}

	/// <summary>
	/// Loads the class at an asset path when this runs, e.g. another mod's "/Game/Mods/Other/ModActor.ModActor_C".
	/// Returns null when the class doesn't exist or isn't a T, so optional dependencies can't break the mod.
	/// </summary>
	[Intrinsic("load-class")]
	public static TSubclassOf<T>? LoadClass<T>(string path) where T : class
	{
		throw FName.Stub();
	}

	/// <summary>A reference to an existing game object (asset) by path, resolved at load time.</summary>
	[Intrinsic("object-at")]
	public static T ObjectAt<T>(string path) where T : class
	{
		throw FName.Stub();
	}
}
