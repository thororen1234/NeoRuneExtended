using System;

namespace NeoRune;

/// <summary>
/// Generates a mod type as an asset at a fixed package path, e.g. "/Game/Mods/BlueprintLoader/BPI_ModSettings", instead of
/// in the mod's own folder: for assets other mods expect at a path, like a loader's interfaces. The asset is named after
/// the path's last part. Works on classes, interfaces (a Blueprint interface), structs and enums (a user enum: name its
/// values with [UName] when they must match an existing enum's).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Enum, Inherited = false)]
public sealed class AssetAttribute : Attribute
{
	public string PackagePath { get; }

	/// <summary>A struct's GUID, when it must match an existing struct's ("26F25DAE-4128-2713-7E60-4B87835D93B1").</summary>
	public string? Guid { get; set; }

	public AssetAttribute(string packagePath)
	{
		PackagePath = packagePath;
	}
}
