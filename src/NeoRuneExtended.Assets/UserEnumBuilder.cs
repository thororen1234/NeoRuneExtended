using System;
using System.Collections.Generic;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;

namespace NeoRuneExtended.Assets;

/// <summary>
/// Writes a user enum (UserDefinedEnum), as the Blueprint editor makes one: namespaced values "Enum::Value" numbered from 0,
/// and the "Enum::Enum_MAX" value after them.
/// </summary>
public sealed class UserEnumBuilder
{
	private readonly EnumExport export;

	private const string UserDefinedEnumClass = "/Script/Engine.UserDefinedEnum";

	public PackageBuilder Package { get; }

	public string EnumName { get; }

	public UserEnumBuilder(string packageName, string enumName)
	{
		Package = new PackageBuilder(packageName);
		EnumName = enumName;
		PackageBuilder package = Package;
		export = new EnumExport
		{
			ObjectName = package.Name(enumName),
			ClassIndex = package.ImportClass(UserDefinedEnumClass),
			SuperIndex = FPackageIndex.FromRawIndex(0),
			TemplateIndex = package.ImportDefaultObject(UserDefinedEnumClass),
			OuterIndex = FPackageIndex.FromRawIndex(0),
			ObjectFlags = (EObjectFlags.RF_Public | EObjectFlags.RF_Standalone | EObjectFlags.RF_Transactional),
			bIsAsset = true,
			Data = new List<PropertyData>(),
			Enum = new UEnum
			{
				Names = new List<Tuple<FName, long>>(),
				CppForm = ECppForm.Namespaced
			},
			Extras = Array.Empty<byte>()
		};
		package.AddExport(export);
	}

	/// <summary>Adds the next value: its name without the enum's (e.g. "NewEnumerator0").</summary>
	public void AddValue(string name, long value)
	{
		export.Enum.Names.Add(Tuple.Create(Package.Name(EnumName + "::" + name), value));
	}

	public string Write(string directory)
	{
		long max = 0;
		foreach (Tuple<FName, long> name in export.Enum.Names)
		{
			max = Math.Max(max, name.Item2 + 1);
		}
		export.Enum.Names.Add(Tuple.Create(Package.Name(EnumName + "::" + EnumName + "_MAX"), max));
		export.SerializationBeforeSerializationDependencies = new List<FPackageIndex>();
		export.CreateBeforeSerializationDependencies = new List<FPackageIndex>();
		export.SerializationBeforeCreateDependencies = new List<FPackageIndex> { export.ClassIndex, export.TemplateIndex };
		export.CreateBeforeCreateDependencies = new List<FPackageIndex>();
		return Package.Write(directory, EnumName);
	}
}
