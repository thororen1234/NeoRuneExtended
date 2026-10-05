using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.FieldTypes;
using UAssetAPI.UnrealTypes;

namespace NeoRuneExtended.Assets;

public sealed class PackageBuilder
{
	public const string CoreUObject = "/Script/CoreUObject";

	private readonly Dictionary<string, FPackageIndex> importsByPath = new Dictionary<string, FPackageIndex>();

	private static readonly (string Name, int Version)[] CustomVersions = new(string, int)[9]
	{
		("FCoreObjectVersion", 4),
		("FUE5SpecialProjectStreamObjectVersion", 9),
		("FFortniteMainBranchObjectVersion", 207),
		("FUE5MainStreamObjectVersion", 121),
		("FReleaseObjectVersion", 44),
		("FBlueprintsObjectVersion", 10),
		("FFrameworkObjectVersion", 37),
		("FUE5ReleaseStreamObjectVersion", 56),
		("FFortniteReleaseBranchCustomObjectVersion", 17)
	};

	public UAsset Asset { get; }

	public string PackageName { get; }

	public PackageBuilder(string packageName)
	{
		PackageName = packageName;
		Asset = LoadSeed();
		Asset.ClearNameIndexList();
		Asset.Imports.Clear();
		Asset.Exports.Clear();
		Asset.DependsMap = new List<int[]>();
		Asset.SoftObjectPathList?.Clear();
		Asset.FolderName = new FString(packageName);
		Asset.CustomVersionContainer = CustomVersions.Select(((string Name, int Version) v) => new CustomVersion(v.Name, v.Version)
		{
			IsSerialized = true
		}).ToList();
		Name("None");
	}

	private static UAsset LoadSeed()
	{
		Assembly assembly = typeof(PackageBuilder).Assembly;
		MemoryStream memoryStream = new MemoryStream();
		string[] array = new string[2] { "seed.uasset", "seed.uexp" };
		foreach (string name in array)
		{
			using Stream stream = assembly.GetManifestResourceStream(name);
			stream.CopyTo(memoryStream);
		}
		memoryStream.Position = 0L;
		UAsset uAsset = new UAsset(EngineVersion.VER_UE5_6)
		{
			UseSeparateBulkDataFiles = true
		};
		uAsset.Read(new AssetBinaryReader(memoryStream, inLoadUexp: true, uAsset));
		return uAsset;
	}

	public FName Name(string value, int number = 0)
	{
		Asset.AddNameReference(new FString(value));
		return new FName(Asset, value, number);
	}

	public FPackageIndex ImportPackage(string package)
	{
		return Import(package, "/Script/CoreUObject", "Package", FPackageIndex.FromRawIndex(0), package);
	}

	public FPackageIndex ImportClass(string classPath)
	{
		(string Package, string Object) tuple = Split(classPath);
		string item = tuple.Package;
		string item2 = tuple.Object;
		bool flag = item.StartsWith("/Script/");
		return Import(classPath, flag ? "/Script/CoreUObject" : "/Script/Engine", flag ? "Class" : "BlueprintGeneratedClass", ImportPackage(item), item2);
	}

	public FPackageIndex ImportStruct(string structPath)
	{
		(string Package, string Object) tuple = Split(structPath);
		string item = tuple.Package;
		string item2 = tuple.Object;
		bool flag = item.StartsWith("/Script/");
		return Import(structPath, "/Script/CoreUObject", flag ? "ScriptStruct" : "UserDefinedStruct", ImportPackage(item), item2);
	}

	public FPackageIndex ImportEnum(string enumPath)
	{
		(string Package, string Object) tuple = Split(enumPath);
		string item = tuple.Package;
		string item2 = tuple.Object;
		bool flag = item.StartsWith("/Script/");
		return Import(enumPath, "/Script/CoreUObject", flag ? "Enum" : "UserDefinedEnum", ImportPackage(item), item2);
	}

	public FPackageIndex ImportFunction(string functionPath, bool delegateSignature = false)
	{
		int num = functionPath.LastIndexOf(':');
		if (num < 0)
		{
			num = functionPath.LastIndexOf('.');
		}
		string text = functionPath.Substring(0, num);
		int num2 = num + 1;
		string objectName = functionPath.Substring(num2, functionPath.Length - num2);
		FPackageIndex outer = (text.Contains('.') ? ImportClass(text) : ImportPackage(text));
		return Import(functionPath, "/Script/CoreUObject", delegateSignature ? "DelegateFunction" : "Function", outer, objectName);
	}

	public FPackageIndex ImportDefaultObject(string classPath)
	{
		var (text, text2) = Split(classPath);
		text.StartsWith("/Script/");
		var (classPackage, className) = Split(classPath);
		return Import(text + ".Default__" + text2, classPackage, className, ImportPackage(text), "Default__" + text2);
	}

	public FPackageIndex ImportObject(string objectPath, string classPath)
	{
		var (package, objectName) = Split(objectPath);
		var (classPackage, className) = Split(classPath);
		return Import(objectPath, classPackage, className, ImportPackage(package), objectName);
	}

	private FPackageIndex Import(string key, string classPackage, string className, FPackageIndex outer, string objectName)
	{
		if (importsByPath.TryGetValue(key, out FPackageIndex value))
		{
			return value;
		}
		Import li = new Import(Name(classPackage), Name(className), outer, Name(objectName), importOptional: false);
		value = Asset.AddImport(li);
		importsByPath[key] = value;
		return value;
	}

	public static (string Package, string Object) Split(string path)
	{
		int num = path.LastIndexOf('.');
		if (num < 0)
		{
			throw new ArgumentException("'" + path + "' is not an object path (expected Package.Object)");
		}
		string item = path.Substring(0, num);
		int num2 = num + 1;
		return (Package: item, Object: path.Substring(num2, path.Length - num2));
	}

	public FPackageIndex AddExport(Export export)
	{
		export.Asset = Asset;
		export.PackageGuid = Guid.Empty;
		Asset.Exports.Add(export);
		Asset.DependsMap.Add(Array.Empty<int>());
		return FPackageIndex.FromExport(Asset.Exports.Count - 1);
	}

	public FPropertyTypeName TypeName(params (string Name, int InnerCount)[] nodes)
	{
		return new FPropertyTypeName(nodes.Select(((string Name, int InnerCount) n) => new FPropertyTypeNameNode
		{
			Name = Name(n.Name),
			InnerCount = n.InnerCount
		}).ToList(), shouldSerialize: true);
	}

	public FProperty Property(string name, UType type, EPropertyFlags flags)
	{
		FProperty fProperty;
		if (type is UType.Prim prim)
		{
			string kind = prim.Kind;
			if (!(kind == "BoolProperty"))
			{
				if (kind == "ByteProperty")
				{
					fProperty = new FByteProperty
					{
						Enum = FPackageIndex.FromRawIndex(0)
					};
				}
				else
				{
					bool flag;
					switch (prim.Kind)
					{
					case "StrProperty":
					case "NameProperty":
					case "TextProperty":
						flag = true;
						break;
					default:
						flag = false;
						break;
					}
					fProperty = (flag ? ((FProperty)new FGenericProperty()) : ((FProperty)new FNumericProperty()));
				}
			}
			else
			{
				fProperty = new FBoolProperty
				{
					FieldSize = 1,
					ByteOffset = 0,
					ByteMask = 1,
					FieldMask = byte.MaxValue,
					NativeBool = true,
					Value = true
				};
			}
		}
		else if (!(type is UType.Object obj))
		{
			if (!(type is UType.Class obj2))
			{
				if (!(type is UType.SoftObject softObject))
				{
					if (!(type is UType.SoftClass softClass))
					{
						if (!(type is UType.WeakObject weakObject))
						{
							if (!(type is UType.Interface obj3))
							{
								if (!(type is UType.Struct obj4))
								{
									if (type is UType.Enum obj5)
									{
										if ((object)obj5.Underlying == null)
										{
											fProperty = new FByteProperty
											{
												Enum = ImportEnum(obj5.EnumPath)
											};
										}
										else
										{
											UType.Enum obj6 = obj5;
											fProperty = new FEnumProperty
											{
												Enum = ImportEnum(obj6.EnumPath),
												UnderlyingProp = Property("UnderlyingType", obj6.Underlying, EPropertyFlags.CPF_None)
											};
										}
									}
									else if (!(type is UType.Array array))
									{
										if (!(type is UType.Set set))
										{
											if (!(type is UType.Map map))
											{
												if (!(type is UType.MulticastDelegate multicastDelegate))
												{
													if (!(type is UType.Delegate obj7))
													{
														throw new NotSupportedException(type.ToString());
													}
													fProperty = new FDelegateProperty
													{
														SignatureFunction = ImportFunction(obj7.SignaturePath, delegateSignature: true)
													};
												}
												else
												{
													fProperty = new FMulticastInlineDelegateProperty
													{
														SignatureFunction = ImportFunction(multicastDelegate.SignaturePath, delegateSignature: true)
													};
												}
											}
											else
											{
												fProperty = new FMapProperty
												{
													KeyProp = Property(name + "_Key", map.Key, InnerFlags(flags)),
													ValueProp = Property(name, map.Value, InnerFlags(flags))
												};
											}
										}
										else
										{
											fProperty = new FSetProperty
											{
												ElementProp = Property(name + "_Element", set.Element, InnerFlags(flags))
											};
										}
									}
									else
									{
										fProperty = new FArrayProperty
										{
											Inner = Property(name, array.Inner, InnerFlags(flags))
										};
									}
								}
								else
								{
									fProperty = new FStructProperty
									{
										Struct = ImportStruct(obj4.StructPath)
									};
								}
							}
							else
							{
								fProperty = new FInterfaceProperty
								{
									InterfaceClass = ImportClass(obj3.InterfaceClassPath)
								};
							}
						}
						else
						{
							fProperty = new FWeakObjectProperty
							{
								PropertyClass = ImportClass(weakObject.ClassPath)
							};
						}
					}
					else
					{
						fProperty = new FSoftClassProperty
						{
							PropertyClass = ImportClass("/Script/CoreUObject.Class"),
							MetaClass = ImportClass(softClass.MetaClassPath)
						};
					}
				}
				else
				{
					fProperty = new FSoftObjectProperty
					{
						PropertyClass = ImportClass(softObject.ClassPath)
					};
				}
			}
			else
			{
				fProperty = new FClassProperty
				{
					PropertyClass = ImportClass("/Script/CoreUObject.Class"),
					MetaClass = ImportClass(obj2.MetaClassPath)
				};
			}
		}
		else
		{
			fProperty = new FObjectProperty
			{
				PropertyClass = ImportClass(obj.ClassPath)
			};
		}
		FProperty fProperty2 = fProperty;
		fProperty2.SerializedType = Name(type.PropertyType);
		fProperty2.Name = Name(PropertyName(name));
		fProperty2.Flags = (EObjectFlags)(1 | ((!flags.HasFlag(EPropertyFlags.CPF_Parm)) ? 2097152 : 0));
		fProperty2.ArrayDim = EArrayDim.TArray;
		fProperty2.ElementSize = type.Size;
		fProperty2.PropertyFlags = flags;
		fProperty2.RepNotifyFunc = Name("None");
		fProperty2.BlueprintReplicationCondition = ELifetimeCondition.COND_None;
		return fProperty2;
	}

	/// <summary>
	/// The Unreal name of a property of the mod's (a field, parameter or local). Names are case-insensitive, so one called
	/// "none" is NAME_None, the empty name: bytecode that refers to it finds nothing, and the VM writes its value into an
	/// uninitialized buffer (a struct there crashed the game). Those get another name; <see cref="ScriptBuilder.Pointer"/>
	/// uses the same one.
	/// </summary>
	public static string PropertyName(string name)
	{
		if (!string.Equals(name, "None", StringComparison.OrdinalIgnoreCase))
		{
			return name;
		}
		return name + "_";
	}

	private static EPropertyFlags InnerFlags(EPropertyFlags outer)
	{
		return EPropertyFlags.CPF_None;
	}

	public string Write(string directory, string assetName)
	{
		Directory.CreateDirectory(directory);
		string text = Path.Combine(directory, assetName + ".uasset");
		foreach (Export export in Asset.Exports)
		{
			export.ScriptSerializationStartOffset = 0L;
			export.ScriptSerializationEndOffset = 9L;
		}
		Asset.Generations = new List<FGenerationInfo>
		{
			new FGenerationInfo(Asset.Exports.Count, Asset.GetNameMapIndexList().Count)
		};
		Asset.Write(text);
		foreach (Export export2 in Asset.Exports)
		{
			if (export2 is NormalExport { Data: { Count: >0 } } && !(export2 is StructExport))
			{
				long serialSize = export2.SerialSize;
				byte[] extras = export2.Extras;
				export2.ScriptSerializationEndOffset = serialSize - ((extras != null) ? extras.Length : 0);
			}
		}
		Asset.Generations = new List<FGenerationInfo>
		{
			new FGenerationInfo(Asset.Exports.Count, Asset.GetNameMapIndexList().Count)
		};
		Asset.Write(text);
		return text;
	}
}
