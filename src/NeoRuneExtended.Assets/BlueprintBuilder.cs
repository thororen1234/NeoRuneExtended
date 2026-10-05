using System;
using System.Collections.Generic;
using System.Linq;
using UAssetAPI.ExportTypes;
using UAssetAPI.FieldTypes;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;

namespace NeoRuneExtended.Assets;

public sealed class BlueprintBuilder
{
	private readonly ClassExport cls;

	private readonly NormalExport cdo;

	private readonly List<FunctionBuilder> functions = new List<FunctionBuilder>();

	private readonly List<FProperty> members = new List<FProperty>();

	private readonly Dictionary<string, UType> memberTypes = new Dictionary<string, UType>();

	private readonly List<FPackageIndex> interfaces = new List<FPackageIndex>();

	public PackageBuilder Package { get; }

	public string ClassName { get; }

	public string ParentClassPath { get; }

	public string ClassPath => Package.PackageName + "." + ClassName;

	public FPackageIndex ClassIndex { get; }

	public FPackageIndex CdoIndex { get; }

	public BlueprintBuilder(string packageName, string assetName, string parentClassPath)
	{
		Package = new PackageBuilder(packageName);
		ClassName = assetName + "_C";
		ParentClassPath = parentClassPath;
		PackageBuilder package = Package;
		cls = new ClassExport
		{
			ObjectName = package.Name(ClassName),
			ClassIndex = package.ImportClass("/Script/Engine.BlueprintGeneratedClass"),
			SuperIndex = package.ImportClass(parentClassPath),
			TemplateIndex = package.ImportDefaultObject("/Script/Engine.BlueprintGeneratedClass"),
			OuterIndex = FPackageIndex.FromRawIndex(0),
			ObjectFlags = (EObjectFlags.RF_Public | EObjectFlags.RF_Transactional),
			bIsAsset = true,
			Data = new List<PropertyData>(),
			FuncMap = new TMap<FName, FPackageIndex>(),
			ClassFlags = EClassFlags.CLASS_CompiledFromBlueprint,
			ClassWithin = package.ImportClass("/Script/CoreUObject.Object"),
			ClassConfigName = package.Name("Engine"),
			Interfaces = Array.Empty<SerializedInterfaceReference>(),
			ClassGeneratedBy = FPackageIndex.FromRawIndex(0),
			bCooked = true,
			SuperStruct = package.ImportClass(parentClassPath),
			Field = new UField(),
			ScriptBytecode = Array.Empty<KismetExpression>(),
			Extras = new byte[4]
		};
		ClassIndex = package.AddExport(cls);
		cdo = new NormalExport
		{
			ObjectName = package.Name("Default__" + ClassName),
			ClassIndex = ClassIndex,
			SuperIndex = FPackageIndex.FromRawIndex(0),
			TemplateIndex = package.ImportDefaultObject(parentClassPath),
			OuterIndex = FPackageIndex.FromRawIndex(0),
			ObjectFlags = (EObjectFlags.RF_Public | EObjectFlags.RF_ClassDefaultObject | EObjectFlags.RF_ArchetypeObject),
			Data = new List<PropertyData>(),
			Extras = new byte[4]
		};
		CdoIndex = package.AddExport(cdo);
		cls.ClassDefaultObject = CdoIndex;
		package.Name("Default__" + ClassName);
	}

	/// <summary>Makes the class a Blueprint interface: its functions are declarations with empty bodies.</summary>
	public void MakeInterface()
	{
		cls.ClassFlags |= EClassFlags.CLASS_Interface;
	}

	public void SetDefaultStructBool(string structProperty, string structPath, string field, bool value)
	{
		PackageBuilder package = Package;
		(string Package, string Object) tuple = PackageBuilder.Split(structPath);
		string item = tuple.Package;
		string item2 = tuple.Object;
		StructPropertyData structPropertyData = cdo.Data.OfType<StructPropertyData>().FirstOrDefault((StructPropertyData d) => d.Name.ToString() == structProperty);
		if (structPropertyData == null)
		{
			StructPropertyData structPropertyData2 = new StructPropertyData(package.Name(structProperty));
			structPropertyData2.StructType = package.Name(item2);
			structPropertyData2.SerializeNone = true;
			structPropertyData2.Value = new List<PropertyData>();
			structPropertyData2.PropertyTypeName = package.TypeName(("StructProperty", 1), (item2, 1), (item, 0));
			structPropertyData = structPropertyData2;
			cdo.Data.Add(structPropertyData);
		}
		structPropertyData.Value.RemoveAll((PropertyData d) => d.Name.ToString() == field);
		structPropertyData.Value.Add(new BoolPropertyData(package.Name(field))
		{
			Value = value,
			PropertyTypeName = package.TypeName(("BoolProperty", 0))
		});
	}

	public void AddVariable(string name, UType type)
	{
		members.Add(Package.Property(name, type, EPropertyFlags.CPF_Edit | EPropertyFlags.CPF_BlueprintVisible | EPropertyFlags.CPF_DisableEditOnInstance));
		memberTypes[name] = type;
	}

	public bool TryGetVariable(string name, out UType type)
	{
		return memberTypes.TryGetValue(name, out type);
	}

	public void AddInterface(string interfaceClassPath)
	{
		FPackageIndex index = Package.ImportClass(interfaceClassPath);
		if (!interfaces.Any((FPackageIndex i) => i.Index == index.Index))
		{
			interfaces.Add(index);
			cls.Interfaces = interfaces.Select((FPackageIndex i) => new SerializedInterfaceReference(i.Index, 0, bImplementedByK2: true)).ToArray();
		}
	}

	public FunctionBuilder AddFunction(string name, EFunctionFlags flags, string? overridesPath = null)
	{
		FunctionBuilder functionBuilder = new FunctionBuilder(this, name, flags, overridesPath);
		functions.Add(functionBuilder);
		cls.FuncMap.Add(Package.Name(name), functionBuilder.Index);
		return functionBuilder;
	}

	public string Write(string directory)
	{
		PackageBuilder package = Package;
		foreach (FunctionBuilder function in functions)
		{
			function.Finish();
		}
		cls.Children = functions.Select((FunctionBuilder f) => f.Index).ToArray();
		cls.LoadedProperties = members.ToArray();
		cls.SerializationBeforeSerializationDependencies = new List<FPackageIndex> { cls.SuperIndex, cdo.TemplateIndex };
		cls.SerializationBeforeSerializationDependencies.AddRange(interfaces);
		cls.CreateBeforeSerializationDependencies = functions.Select((FunctionBuilder f) => f.Index).ToList();
		cls.SerializationBeforeCreateDependencies = new List<FPackageIndex> { cls.ClassIndex, cls.TemplateIndex };
		cls.CreateBeforeCreateDependencies = new List<FPackageIndex> { cls.SuperIndex };
		foreach (FProperty member in members)
		{
			AddTypeDeps(member, cls.CreateBeforeSerializationDependencies);
		}
		cdo.SerializationBeforeSerializationDependencies = new List<FPackageIndex>();
		cdo.CreateBeforeSerializationDependencies = new List<FPackageIndex>();
		cdo.SerializationBeforeCreateDependencies = new List<FPackageIndex> { ClassIndex, cdo.TemplateIndex };
		cdo.CreateBeforeCreateDependencies = new List<FPackageIndex>();
		string className = ClassName;
		string assetName = className.Substring(0, className.Length - 2);
		return package.Write(directory, assetName);
	}

	internal static void AddTypeDeps(FProperty prop, List<FPackageIndex> deps)
	{
		if (!(prop is FClassProperty fClassProperty))
		{
			if (!(prop is FObjectProperty fObjectProperty))
			{
				if (!(prop is FStructProperty fStructProperty))
				{
					if (!(prop is FEnumProperty fEnumProperty))
					{
						if (!(prop is FByteProperty fByteProperty))
						{
							if (!(prop is FArrayProperty fArrayProperty))
							{
								if (!(prop is FSetProperty fSetProperty))
								{
									if (!(prop is FMapProperty fMapProperty))
									{
										if (!(prop is FDelegateProperty fDelegateProperty))
										{
											if (prop is FInterfaceProperty fInterfaceProperty)
											{
												add(fInterfaceProperty.InterfaceClass);
											}
										}
										else
										{
											add(fDelegateProperty.SignatureFunction);
										}
									}
									else
									{
										AddTypeDeps(fMapProperty.KeyProp, deps);
										AddTypeDeps(fMapProperty.ValueProp, deps);
									}
								}
								else
								{
									AddTypeDeps(fSetProperty.ElementProp, deps);
								}
							}
							else
							{
								AddTypeDeps(fArrayProperty.Inner, deps);
							}
						}
						else
						{
							add(fByteProperty.Enum);
						}
					}
					else
					{
						add(fEnumProperty.Enum);
					}
				}
				else
				{
					add(fStructProperty.Struct);
				}
			}
			else
			{
				add(fObjectProperty.PropertyClass);
			}
		}
		else
		{
			add(fClassProperty.MetaClass);
		}
		void add(FPackageIndex? i)
		{
			if (i != null && i.Index != 0 && !deps.Any((FPackageIndex d) => d.Index == i.Index))
			{
				deps.Add(i);
			}
		}
	}
}
