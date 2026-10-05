using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NeoRuneExtended.Discovery;

public sealed class ReflectionDumper
{
	private const int PropOffset = 72;

	private const int PropExtra = 120;

	private const int PropExtra2 = 128;

	private const int FunctionFlagsOffset = 176;

	private readonly GameProcess p;

	private readonly ObjectReader r;

	private readonly Action<string> log;

	private int propFlags = -1;

	private int propArrayDim = -1;

	private int enumNames = -1;

	private static readonly HashSet<string> ClassKinds = new HashSet<string> { "Class", "ASClass", "BlueprintGeneratedClass", "WidgetBlueprintGeneratedClass", "AnimBlueprintGeneratedClass" };

	private static readonly HashSet<string> StructKinds = new HashSet<string> { "ScriptStruct", "ASStruct", "UserDefinedStruct" };

	private static readonly HashSet<string> EnumKinds = new HashSet<string> { "Enum", "UserDefinedEnum" };

	public ReflectionDumper(GameProcess process, Action<string>? log = null)
	{
		p = process;
		this.log = log ?? ((Action<string>)delegate
		{
		});
		r = new ObjectReader(process);
		this.log($"{r.Objects.Count} objects");
		Calibrate();
	}

	private IEnumerable<ulong> Props(ulong structObj)
	{
		for (ulong f = p.U64(structObj + 80); f != 0L; f = p.U64(f + 24))
		{
			yield return f;
		}
	}

	private string FieldClass(ulong f)
	{
		ulong num = p.U64(f + 8);
		if (num != 0L)
		{
			return r.FName(num);
		}
		return "?";
	}

	private string FieldName(ulong f)
	{
		return r.FName(f + 32);
	}

	private ulong FindObject(string className, string path)
	{
		return r.Objects.FirstOrDefault((ulong o) => r.Name(o) == path.Split('.', ':')[^1] && r.ClassName(o) == className && r.PathName(o) == path);
	}

	private ulong FindFunction(string ownerPath, string name)
	{
		ulong num = FindObject("Class", ownerPath);
		if (num == 0L)
		{
			throw new InvalidOperationException("calibration class " + ownerPath + " not found");
		}
		for (ulong num2 = p.U64(num + 72); num2 != 0L; num2 = p.U64(num2 + 40))
		{
			if (r.Name(num2) == name)
			{
				return num2;
			}
		}
		throw new InvalidOperationException($"calibration function {ownerPath}:{name} not found");
	}

	private void Calibrate()
	{
		ulong num = FindFunction("/Script/Engine.GameplayStatics", "SaveGameToSlot");
		Dictionary<string, ulong> dictionary = Props(num).ToDictionary(FieldName);
		ulong num2 = dictionary["ReturnValue"];
		ulong num3 = dictionary["SlotName"];
		ulong num4 = dictionary["UserIndex"];
		for (int i = 48; i < 72; i += 8)
		{
			ulong num5 = p.U64(num2 + (ulong)i);
			ulong num6 = p.U64(num3 + (ulong)i);
			ulong num7 = p.U64(num4 + (ulong)i);
			if ((num5 & 0x580) == 1408 && (num6 & 0x80) != 0L && (num6 & 0x400) == 0L && (num7 & 0x80) != 0L && (num7 & 0x500) == 0)
			{
				propFlags = i;
				break;
			}
		}
		for (int j = 48; j < 72; j += 4)
		{
			if (p.U32(num4 + (ulong)j) == 1 && p.U32((ulong)((long)num4 + (long)j + 4)) == 4 && p.U32((ulong)((long)num3 + (long)j + 4)) == 16)
			{
				propArrayDim = j;
				break;
			}
		}
		if (propFlags < 0 || propArrayDim < 0)
		{
			StringBuilder stringBuilder = new StringBuilder("could not calibrate FProperty layout:\n");
			(string, ulong)[] array = new(string, ulong)[3]
			{
				("ReturnValue", num2),
				("SlotName", num3),
				("UserIndex", num4)
			};
			for (int k = 0; k < array.Length; k++)
			{
				(string, ulong) tuple = array[k];
				string item = tuple.Item1;
				ulong item2 = tuple.Item2;
				byte[] value = p.Read(item2, 128);
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder3 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(6, 2, stringBuilder2);
				handler.AppendLiteral("  ");
				handler.AppendFormatted(item);
				handler.AppendLiteral(" (");
				handler.AppendFormatted(FieldClass(item2));
				handler.AppendLiteral("):");
				stringBuilder3.Append(ref handler);
				for (int l = 40; l < 128; l += 8)
				{
					stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder4 = stringBuilder2;
					handler = new StringBuilder.AppendInterpolatedStringHandler(2, 2, stringBuilder2);
					handler.AppendLiteral(" ");
					handler.AppendFormatted(l, "x2");
					handler.AppendLiteral(":");
					handler.AppendFormatted(BitConverter.ToUInt64(value, l), "x");
					stringBuilder4.Append(ref handler);
				}
				stringBuilder.AppendLine();
			}
			throw new InvalidOperationException(stringBuilder.ToString());
		}
		uint num8 = p.U32(num + 176);
		if ((num8 & 0x2400) != 9216)
		{
			throw new InvalidOperationException($"UFunction flags offset check failed (0x{num8:x})");
		}
		ulong num9 = FindObject("Enum", "/Script/Engine.ESpawnActorCollisionHandlingMethod");
		for (int m = 48; m < 128; m += 8)
		{
			ulong num10 = p.U64(num9 + (ulong)m);
			int num11 = p.I32((ulong)((long)num9 + (long)m + 8));
			if (num10 != 0L && num11 > 1 && num11 < 16 && r.FName(num10).StartsWith("ESpawnActorCollisionHandlingMethod::"))
			{
				enumNames = m;
				break;
			}
		}
		if (enumNames < 0)
		{
			throw new InvalidOperationException("could not calibrate UEnum layout");
		}
		log($"calibrated: PropertyFlags=+0x{propFlags:x} ArrayDim=+0x{propArrayDim:x} EnumNames=+0x{enumNames:x}");
	}

	private string Path(ulong o)
	{
		if (o == 0L || !r.ObjectSet.Contains(o))
		{
			return "?";
		}
		return r.PathName(o);
	}

	private TypeRef Type(ulong f)
	{
		string text = FieldClass(f);
		ulong num = p.U64(f + 120);
		ulong num2 = p.U64(f + 128);
		switch (text)
		{
		case "BoolProperty":
			return new TypeRef
			{
				Kind = "bool"
			};
		case "Int8Property":
			return new TypeRef
			{
				Kind = "int8"
			};
		case "Int16Property":
			return new TypeRef
			{
				Kind = "int16"
			};
		case "IntProperty":
			return new TypeRef
			{
				Kind = "int"
			};
		case "Int64Property":
			return new TypeRef
			{
				Kind = "int64"
			};
		case "UInt16Property":
			return new TypeRef
			{
				Kind = "uint16"
			};
		case "UInt32Property":
			return new TypeRef
			{
				Kind = "uint32"
			};
		case "UInt64Property":
			return new TypeRef
			{
				Kind = "uint64"
			};
		case "FloatProperty":
			return new TypeRef
			{
				Kind = "float"
			};
		case "DoubleProperty":
			return new TypeRef
			{
				Kind = "double"
			};
		case "StrProperty":
			return new TypeRef
			{
				Kind = "string"
			};
		case "NameProperty":
			return new TypeRef
			{
				Kind = "name"
			};
		case "TextProperty":
			return new TypeRef
			{
				Kind = "text"
			};
		case "ByteProperty":
			if (num == 0L)
			{
				return new TypeRef
				{
					Kind = "byte"
				};
			}
			return new TypeRef
			{
				Kind = "enum",
				Path = Path(num),
				Inner = new TypeRef
				{
					Kind = "byte"
				}
			};
		case "EnumProperty":
			return new TypeRef
			{
				Kind = "enum",
				Path = Path(num2),
				Inner = Type(num)
			};
		case "StructProperty":
			return new TypeRef
			{
				Kind = "struct",
				Path = Path(num)
			};
		case "ObjectProperty":
		case "ObjectPtrProperty":
			return new TypeRef
			{
				Kind = "object",
				Path = Path(num)
			};
		case "WeakObjectProperty":
			return new TypeRef
			{
				Kind = "weakobject",
				Path = Path(num)
			};
		case "LazyObjectProperty":
			return new TypeRef
			{
				Kind = "lazyobject",
				Path = Path(num)
			};
		case "SoftObjectProperty":
			return new TypeRef
			{
				Kind = "softobject",
				Path = Path(num)
			};
		case "ClassProperty":
		case "ClassPtrProperty":
			return new TypeRef
			{
				Kind = "class",
				Path = Path(num),
				Meta = Path(num2)
			};
		case "SoftClassProperty":
			return new TypeRef
			{
				Kind = "softclass",
				Path = Path(num),
				Meta = Path(num2)
			};
		case "InterfaceProperty":
			return new TypeRef
			{
				Kind = "interface",
				Path = Path(num)
			};
		case "ArrayProperty":
			return new TypeRef
			{
				Kind = "array",
				Inner = ((num2 != 0L) ? Type(num2) : new TypeRef())
			};
		case "SetProperty":
			return new TypeRef
			{
				Kind = "set",
				Inner = Type(num)
			};
		case "MapProperty":
			return new TypeRef
			{
				Kind = "map",
				Inner = Type(num),
				Value = Type(num2)
			};
		case "DelegateProperty":
			return new TypeRef
			{
				Kind = "delegate",
				Path = Path(num)
			};
		case "MulticastInlineDelegateProperty":
		case "MulticastSparseDelegateProperty":
		case "MulticastDelegateProperty":
			return new TypeRef
			{
				Kind = "multicastdelegate",
				Path = Path(num),
				Raw = text
			};
		case "FieldPathProperty":
			return new TypeRef
			{
				Kind = "fieldpath"
			};
		case "OptionalProperty":
			return new TypeRef
			{
				Kind = "optional",
				Inner = Type(num)
			};
		default:
			return new TypeRef
			{
				Kind = "unknown",
				Raw = text
			};
		}
	}

	private ApiProperty Property(ulong f)
	{
		return new ApiProperty
		{
			Name = FieldName(f),
			Type = Type(f),
			Offset = p.I32(f + 72),
			Flags = p.U64(f + (ulong)propFlags)
		};
	}

	public ApiDump Dump(Func<string, bool>? packageFilter = null)
	{
		ApiDump apiDump = new ApiDump();
		apiDump.Game.ExeStamp = GameInstall.ExeStamp(p.ExePath);
		apiDump.Game.BuildId = GameInstall.SteamBuildId(p.ExePath);
		string fullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(p.ExePath), "..", "..", ".."));
		apiDump.Game.GameVersion = GameInstall.PackageVersion(fullPath);
		apiDump.Game.DumpedAt = DateTime.UtcNow.ToString("O");
		foreach (ulong @object in r.Objects)
		{
			string text = r.ClassName(@object);
			if (text.EndsWith("DelegateFunction") && r.Outer(@object) != 0L && r.Outer(r.Outer(@object)) == 0L)
			{
				if (packageFilter == null || packageFilter(r.PackageName(@object)))
				{
					apiDump.Types.Add(new ApiType
					{
						Path = r.PathName(@object),
						MetaClass = text,
						Kind = "delegate",
						Functions = new List<ApiFunction>
						{
							new ApiFunction
							{
								Name = r.Name(@object),
								Flags = p.U32(@object + 176),
								Params = Props(@object).Select(Property).ToList()
							}
						}
					});
				}
				continue;
			}
			bool flag = ClassKinds.Contains(text);
			bool flag2 = StructKinds.Contains(text);
			bool flag3 = EnumKinds.Contains(text);
			if ((!flag && !flag2 && !flag3) || r.Name(@object).StartsWith("Default__") || r.Name(@object).StartsWith("REINST_") || r.Name(@object).StartsWith("SKEL_"))
			{
				continue;
			}
			string arg = r.PackageName(@object);
			if (packageFilter != null && !packageFilter(arg))
			{
				continue;
			}
			ApiType apiType = new ApiType
			{
				Path = r.PathName(@object),
				MetaClass = text,
				Kind = (flag ? "class" : (flag2 ? "struct" : "enum"))
			};
			if (flag3)
			{
				apiType.Values = new List<ApiEnumValue>();
				ulong num = p.U64(@object + (ulong)enumNames);
				int num2 = p.I32((ulong)((long)@object + (long)enumNames + 8));
				if (num2 > 0 && num2 < 100000 && num != 0L)
				{
					byte[] array = p.Read(num, 16 * num2);
					int num3 = 0;
					while (array != null && num3 < num2)
					{
						apiType.Values.Add(new ApiEnumValue
						{
							Name = r.FName(num + (ulong)(16L * (long)num3)),
							Value = BitConverter.ToInt64(array, 16 * num3 + 8)
						});
						num3++;
					}
				}
			}
			else
			{
				ulong num4 = p.U64(@object + 64);
				apiType.Super = ((num4 != 0L && r.ObjectSet.Contains(num4)) ? r.PathName(num4) : null);
				apiType.Size = p.I32(@object + 88);
				apiType.Properties = Props(@object).Select(Property).ToList();
				if (flag)
				{
					apiType.Functions = new List<ApiFunction>();
					ulong num5 = p.U64(@object + 72);
					while (num5 != 0L && r.ObjectSet.Contains(num5))
					{
						if (r.ClassName(num5).Contains("Function"))
						{
							apiType.Functions.Add(new ApiFunction
							{
								Name = r.Name(num5),
								Flags = p.U32(num5 + 176),
								Params = Props(num5).Select(Property).ToList()
							});
						}
						num5 = p.U64(num5 + 40);
					}
				}
			}
			apiDump.Types.Add(apiType);
		}
		apiDump.Types.Sort((ApiType a, ApiType b) => string.CompareOrdinal(a.Path, b.Path));
		log($"{apiDump.Types.Count} types");
		return apiDump;
	}
}
