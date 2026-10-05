using System;
using System.Collections.Generic;
using System.Linq;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;

namespace NeoRuneExtended.Assets;

public static class ModInfoBuilder
{
	private const string Loader = "/Game/Mods/BlueprintLoader";

	private const string ModInfoClass = "/Game/Mods/BlueprintLoader/BP_ModInfo.BP_ModInfo_C";

	private const string SettingStruct = "S_ModSetting";

	private const string SettingStructGuid = "26f25dae-4128-2713-7e60-4b87835d93b1";

	private const string SettingTypeEnum = "E_ModSettingType";

	private const string Type = "Type_2_B00CB93D43E746B0BE3E249C5B65F9D2";

	private const string Id = "Id_4_59D80CF146C77FF75AC65F96B5941833";

	private const string Label = "Label_6_C0961F4247FF28D6064EFA8A70420A61";

	private const string Description = "Description_8_F1348120455495654CFDFA97A03FD6DE";

	private const string DefaultOn = "DefaultOn_10_124D316B43295296E470F18C764EC644";

	private const string DefaultValue = "DefaultValue_12_B6229FAC4BC312D30C05C0B6BE0B4254";

	private const string Min = "Min_14_2290187042A74A0D0833099EAD348328";

	private const string Max = "Max_16_9FAA820945BBE75261B51E9040738BDC";

	private const string Step = "Step_18_1ED4C3A742C04EE5298AF5884899ED10";

	private const string ValueWidth = "ValueWidth_20_F730978746B88C346ED8658E3FCDFBE6";

	private const string Options = "Options_22_1377EBB34B4849D01796E68B189A0AB2";

	private const string DefaultOption = "DefaultOption_24_4B08081147723E8A9D6A8795157E51A6";

	private const string Url = "Url_26_F68A84A743BBB53209453BA9D2C4AFBF";

	private const string ButtonText = "ButtonText_28_3F625A794A47F1486490DDB5B94D9C5E";

	private const string Height = "Height_30_52E08D6A41E57747FA39F0AB511AFE6E";

	private const string DefaultKey = "DefaultKey_32_9411F67C406AC500CC7C6F89B45A04E4";

	private const string DefaultText = "DefaultText_34_54C30B5446479E771CE3FE8A5ECDF36D";

	private const string DefaultColour = "DefaultColour_36_B8031080484A7124D758C29E0F455300";

	private const string WidgetClass = "WidgetClass_38_447425D74FC8BFF1B088CCBC7B7D1024";

	private const string Percentage = "Percentage_40_8024A1B04F10DD1177E88BB146EE578B";

	private const string HexInput = "HexInput_42_DB63640B4B8BB8A52B0CD1864485944A";

	private const string Placeholder = "Placeholder_44_615145644B6DC2DB2508D3ADF1A38DB2";

	private const string SecondaryKey = "SecondaryKey_46_B8190CA744AEB23447C30AB5ED9A0C45";

	public static string Write(string modRoot, ModInfoData info, string directory)
	{
		PackageBuilder p = new PackageBuilder(modRoot + "/ModInfo");
		List<PropertyData> data = new List<PropertyData>();
		str("ModName", info.ModName);
		str("Version", info.Version);
		str("Author", info.Author);
		str("AuthorUrl", info.AuthorUrl);
		str("Description", info.Description);
		if (info.Settings.Count > 0)
		{
			data.Add(new ArrayPropertyData(p.Name("Settings"))
			{
				ArrayType = p.Name("StructProperty"),
				PropertyTypeName = p.TypeName(("ArrayProperty", 1), ("StructProperty", 2), ("S_ModSetting", 1), ("/Game/Mods/BlueprintLoader/S_ModSetting", 0), ("26f25dae-4128-2713-7e60-4b87835d93b1", 0)),
				Value = ((IEnumerable<ModSettingData>)info.Settings).Select((Func<ModSettingData, PropertyData>)((ModSettingData s) => Setting(p, s))).ToArray()
			});
		}
		NormalExport normalExport = new NormalExport
		{
			ObjectName = p.Name("ModInfo"),
			ClassIndex = p.ImportClass("/Game/Mods/BlueprintLoader/BP_ModInfo.BP_ModInfo_C"),
			SuperIndex = FPackageIndex.FromRawIndex(0),
			TemplateIndex = p.ImportDefaultObject("/Game/Mods/BlueprintLoader/BP_ModInfo.BP_ModInfo_C"),
			OuterIndex = FPackageIndex.FromRawIndex(0),
			ObjectFlags = (EObjectFlags.RF_Public | EObjectFlags.RF_Standalone | EObjectFlags.RF_Transactional),
			bIsAsset = true,
			Data = data,
			Extras = Array.Empty<byte>()
		};
		p.AddExport(normalExport);
		normalExport.SerializationBeforeSerializationDependencies = new List<FPackageIndex>();
		normalExport.CreateBeforeSerializationDependencies = (from o in data.OfType<ArrayPropertyData>().SelectMany((ArrayPropertyData a) => a.Value).OfType<StructPropertyData>()
				.SelectMany((StructPropertyData s) => s.Value)
				.OfType<ObjectPropertyData>()
			select o.Value).ToList();
		normalExport.SerializationBeforeCreateDependencies = new List<FPackageIndex> { normalExport.ClassIndex, normalExport.TemplateIndex };
		normalExport.CreateBeforeCreateDependencies = new List<FPackageIndex>();
		return p.Write(directory, "ModInfo");
		void str(string name, string? value)
		{
			if (!string.IsNullOrEmpty(value))
			{
				data.Add(Str(p, name, value));
			}
		}
	}

	private static StructPropertyData Setting(PackageBuilder p, ModSettingData s)
	{
		List<PropertyData> v = new List<PropertyData>
		{
			new BytePropertyData(p.Name("Type_2_B00CB93D43E746B0BE3E249C5B65F9D2"))
			{
				ByteType = BytePropertyType.FName,
				EnumType = p.Name("E_ModSettingType"),
				EnumValue = p.Name($"{"E_ModSettingType"}::NewEnumerator{(int)s.Type}"),
				PropertyTypeName = p.TypeName(("ByteProperty", 1), ("E_ModSettingType", 1), ("/Game/Mods/BlueprintLoader/E_ModSettingType", 0))
			}
		};
		str("Id_4_59D80CF146C77FF75AC65F96B5941833", s.Id);
		str("Label_6_C0961F4247FF28D6064EFA8A70420A61", s.Label);
		str("Description_8_F1348120455495654CFDFA97A03FD6DE", s.Description);
		flag("DefaultOn_10_124D316B43295296E470F18C764EC644", s.DefaultOn);
		number("DefaultValue_12_B6229FAC4BC312D30C05C0B6BE0B4254", s.DefaultValue, 0.0);
		number("Min_14_2290187042A74A0D0833099EAD348328", s.Min, 0.0);
		number("Max_16_9FAA820945BBE75261B51E9040738BDC", s.Max, 1.0);
		number("Step_18_1ED4C3A742C04EE5298AF5884899ED10", s.Step, 0.0);
		number("ValueWidth_20_F730978746B88C346ED8658E3FCDFBE6", s.ValueWidth, 0.0);
		if (s.Options.Count > 0)
		{
			v.Add(new ArrayPropertyData(p.Name("Options_22_1377EBB34B4849D01796E68B189A0AB2"))
			{
				ArrayType = p.Name("StrProperty"),
				PropertyTypeName = p.TypeName(("ArrayProperty", 1), ("StrProperty", 0)),
				Value = ((IEnumerable<string>)s.Options).Select((Func<string, PropertyData>)((string o) => new StrPropertyData(p.Name("Options_22_1377EBB34B4849D01796E68B189A0AB2"))
				{
					Value = new FString(o)
				})).ToArray()
			});
		}
		if (s.DefaultOption != 0)
		{
			v.Add(new IntPropertyData(p.Name("DefaultOption_24_4B08081147723E8A9D6A8795157E51A6"))
			{
				Value = s.DefaultOption,
				PropertyTypeName = p.TypeName(("IntProperty", 0))
			});
		}
		str("Url_26_F68A84A743BBB53209453BA9D2C4AFBF", s.Url);
		str("ButtonText_28_3F625A794A47F1486490DDB5B94D9C5E", s.ButtonText);
		number("Height_30_52E08D6A41E57747FA39F0AB511AFE6E", s.Height, 16.0);
		key("DefaultKey_32_9411F67C406AC500CC7C6F89B45A04E4", s.DefaultKey);
		str("DefaultText_34_54C30B5446479E771CE3FE8A5ECDF36D", s.DefaultText);
		(float, float, float, float)? defaultColour = s.DefaultColour;
		if (defaultColour.HasValue)
		{
			(float, float, float, float) valueOrDefault = defaultColour.GetValueOrDefault();
			v.Add(new StructPropertyData(p.Name("DefaultColour_36_B8031080484A7124D758C29E0F455300"))
			{
				StructType = p.Name("LinearColor"),
				SerializeNone = true,
				PropertyTagFlags = EPropertyTagFlags.HasBinaryOrNativeSerialize,
				PropertyTypeName = p.TypeName(("StructProperty", 1), ("LinearColor", 1), ("/Script/CoreUObject", 0)),
				Value = new List<PropertyData>
				{
					new LinearColorPropertyData(p.Name("DefaultColour_36_B8031080484A7124D758C29E0F455300"))
					{
						Value = new FLinearColor(valueOrDefault.Item1, valueOrDefault.Item2, valueOrDefault.Item3, valueOrDefault.Item4)
					}
				}
			});
		}
		if (s.WidgetClass != null)
		{
			v.Add(new ObjectPropertyData(p.Name("WidgetClass_38_447425D74FC8BFF1B088CCBC7B7D1024"))
			{
				Value = p.ImportClass(s.WidgetClass),
				PropertyTypeName = p.TypeName(("ObjectProperty", 0))
			});
		}
		flag("Percentage_40_8024A1B04F10DD1177E88BB146EE578B", s.Percentage);
		flag("HexInput_42_DB63640B4B8BB8A52B0CD1864485944A", s.HexInput);
		str("Placeholder_44_615145644B6DC2DB2508D3ADF1A38DB2", s.Placeholder);
		key("SecondaryKey_46_B8190CA744AEB23447C30AB5ED9A0C45", s.SecondaryKey);
		return new StructPropertyData(p.Name("Settings"))
		{
			StructType = p.Name("S_ModSetting"),
			SerializeNone = true,
			Value = v
		};
		void flag(string name, bool value)
		{
			if (value)
			{
				v.Add(new BoolPropertyData(p.Name(name))
				{
					Value = true,
					PropertyTypeName = p.TypeName(("BoolProperty", 0))
				});
			}
		}
		void key(string name, string? keyName)
		{
			if (!string.IsNullOrEmpty(keyName))
			{
				v.Add(new StructPropertyData(p.Name(name))
				{
					StructType = p.Name("Key"),
					SerializeNone = true,
					PropertyTypeName = p.TypeName(("StructProperty", 1), ("Key", 1), ("/Script/InputCore", 0)),
					Value = new List<PropertyData>
					{
						new NamePropertyData(p.Name("KeyName"))
						{
							Value = p.Name(keyName),
							PropertyTypeName = p.TypeName(("NameProperty", 0))
						}
					}
				});
			}
		}
		void number(string name, double value, double structDefault)
		{
			if (value != structDefault)
			{
				v.Add(new DoublePropertyData(p.Name(name))
				{
					Value = value,
					PropertyTypeName = p.TypeName(("DoubleProperty", 0))
				});
			}
		}
		void str(string name, string? value)
		{
			if (!string.IsNullOrEmpty(value))
			{
				v.Add(Str(p, name, value));
			}
		}
	}

	private static StrPropertyData Str(PackageBuilder p, string name, string value)
	{
		StrPropertyData strPropertyData = new StrPropertyData(p.Name(name));
		strPropertyData.Value = new FString(value);
		strPropertyData.PropertyTypeName = p.TypeName(("StrProperty", 0));
		return strPropertyData;
	}
}
