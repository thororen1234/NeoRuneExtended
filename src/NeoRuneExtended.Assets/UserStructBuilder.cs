using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UAssetAPI.ExportTypes;
using UAssetAPI.FieldTypes;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;

namespace NeoRuneExtended.Assets;

public sealed class UserStructBuilder
{
	private readonly UserDefinedStructExport export;

	private readonly List<FProperty> fields = new List<FProperty>();

	private const string UserDefinedStructClass = "/Script/CoreUObject.UserDefinedStruct";

	public PackageBuilder Package { get; }

	public string StructName { get; }

	public string StructPath => Package.PackageName + "." + StructName;

	/// <param name="guid">The struct's GUID, when it must match an existing struct's; otherwise one made from the path.</param>
	public UserStructBuilder(string packageName, string structName, Guid? guid = null)
	{
		Package = new PackageBuilder(packageName);
		StructName = structName;
		PackageBuilder package = Package;
		export = new UserDefinedStructExport
		{
			ObjectName = package.Name(structName),
			ClassIndex = package.ImportClass("/Script/CoreUObject.UserDefinedStruct"),
			SuperIndex = FPackageIndex.FromRawIndex(0),
			TemplateIndex = package.ImportDefaultObject("/Script/CoreUObject.UserDefinedStruct"),
			OuterIndex = FPackageIndex.FromRawIndex(0),
			ObjectFlags = (EObjectFlags.RF_Public | EObjectFlags.RF_Standalone | EObjectFlags.RF_Transactional),
			bIsAsset = true,
			Data = new List<PropertyData>
			{
				new StructPropertyData(package.Name("Guid"))
				{
					StructType = package.Name("Guid"),
					Value = new List<PropertyData>
					{
						new GuidPropertyData(package.Name("Guid"))
						{
							Value = guid ?? StableGuid(packageName)
						}
					},
					PropertyTypeName = package.TypeName(("StructProperty", 1), ("Guid", 1), ("/Script/CoreUObject", 0))
				}
			},
			SuperStruct = FPackageIndex.FromRawIndex(0),
			Children = Array.Empty<FPackageIndex>(),
			Field = new UField(),
			ScriptBytecode = Array.Empty<KismetExpression>(),
			StructFlags = 0u,
			Extras = Array.Empty<byte>()
		};
		package.AddExport(export);
	}

	public void AddField(string name, UType type)
	{
		fields.Add(Package.Property(name, type, EPropertyFlags.CPF_Edit | EPropertyFlags.CPF_BlueprintVisible));
	}

	private static Guid StableGuid(string path)
	{
		return new Guid(MD5.HashData(Encoding.UTF8.GetBytes(path)));
	}

	public string Write(string directory)
	{
		export.LoadedProperties = fields.ToArray();
		export.SerializationBeforeSerializationDependencies = new List<FPackageIndex>();
		export.CreateBeforeSerializationDependencies = new List<FPackageIndex>();
		foreach (FProperty field in fields)
		{
			BlueprintBuilder.AddTypeDeps(field, export.CreateBeforeSerializationDependencies);
		}
		export.SerializationBeforeCreateDependencies = new List<FPackageIndex> { export.ClassIndex, export.TemplateIndex };
		export.CreateBeforeCreateDependencies = new List<FPackageIndex>();
		return Package.Write(directory, StructName);
	}
}
