using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using NeoRuneExtended.Assets;

namespace NeoRuneExtended.Compiler;

internal static class ModSettings
{
	public static List<ModSettingData> Read(ModCompiler mod, INamedTypeSymbol modActor)
	{
		List<ModSettingData> list = new List<ModSettingData>();
		HashSet<string> ids = new HashSet<string>();
		foreach (AttributeData attribute in modActor.GetAttributes())
		{
			AttributeData a = attribute;
			INamedTypeSymbol attributeClass = a.AttributeClass;
			if (attributeClass == null)
			{
				continue;
			}
			INamedTypeSymbol containingType = attributeClass.ContainingType;
			if (containingType == null || !(containingType.Name == "ModSetting") || containingType.ContainingNamespace.ToDisplayString() != "NeoRune")
			{
				continue;
			}
			Location at = a.ApplicationSyntaxReference?.GetSyntax().GetLocation();
			string description = named<string>("Description", null);
			List<ModSettingData> list2 = list;
			ModSettingData item;
			switch (attributeClass.Name)
			{
			case "HeadingAttribute":
				item = new ModSettingData
				{
					Type = ModSettingType.Heading,
					Label = arg(0),
					Description = description
				};
				break;
			case "TextAttribute":
				item = new ModSettingData
				{
					Type = ModSettingType.Text,
					Label = arg(0),
					Description = description
				};
				break;
			case "SpacerAttribute":
				item = new ModSettingData
				{
					Type = ModSettingType.Spacer,
					Height = ((a.ConstructorArguments[0].Value is double num) ? num : 16.0)
				};
				break;
			case "ToggleAttribute":
				item = new ModSettingData
				{
					Type = ModSettingType.Toggle,
					Id = id(0),
					Label = arg(1),
					Description = description,
					DefaultOn = named<bool>("Default", fallback: false)
				};
				break;
			case "SliderAttribute":
				item = Slider(a, id(0), arg(1), description, at);
				break;
			case "SelectAttribute":
				item = Select(id(0), arg(1), description, a.ConstructorArguments[2].Values.Select((TypedConstant v) => (v.Value as string) ?? "").ToArray(), named<int>("Default", 0), at);
				break;
			case "UrlButtonAttribute":
				item = new ModSettingData
				{
					Type = ModSettingType.UrlButton,
					Label = arg(0),
					Url = arg(1),
					ButtonText = named<string>("ButtonText", null),
					Description = description
				};
				break;
			case "EventButtonAttribute":
				item = new ModSettingData
				{
					Type = ModSettingType.EventButton,
					Id = id(0),
					Label = arg(1),
					ButtonText = named<string>("ButtonText", null),
					Description = description
				};
				break;
			case "KeybindAttribute":
				item = new ModSettingData
				{
					Type = ModSettingType.Keybind,
					Id = id(0),
					Label = arg(1),
					Description = description,
					DefaultKey = named<string>("Default", null),
					SecondaryKey = named<string>("Secondary", null)
				};
				break;
			case "TextInputAttribute":
				item = new ModSettingData
				{
					Type = ModSettingType.TextInput,
					Id = id(0),
					Label = arg(1),
					Description = description,
					DefaultText = named<string>("Default", null),
					Placeholder = named<string>("Placeholder", null)
				};
				break;
			case "ColourAttribute":
			{
				string text = named<string>("Default", null);
				item = new ModSettingData
				{
					Type = ModSettingType.Colour,
					Id = id(0),
					Label = arg(1),
					Description = description,
					HexInput = named<bool>("HexInput", fallback: false),
					DefaultColour = ((text != null) ? LinearColour(text, at) : null),
					// Checked here so a bad colour is a compile error, not a blank swatch in game.
					Options = namedArray("Options").Select(delegate(string o)
					{
						LinearColour(o, at);
						return o;
					}).ToArray()
				};
				break;
			}
			case "WidgetAttribute":
				item = new ModSettingData
				{
					Type = ModSettingType.Widget,
					Id = id(0),
					Description = description,
					WidgetClass = WidgetClass(mod, a.ConstructorArguments[1].Value as INamedTypeSymbol, at)
				};
				break;
			default:
			{
				string name = attributeClass.Name;
				int length = "Attribute".Length;
				throw new CompileError("unknown setting [ModSetting." + name.Substring(0, name.Length - length) + "]", at);
			}
			}
			list2.Add(item);
			string arg(int i)
			{
				return (a.ConstructorArguments[i].Value as string) ?? "";
			}
			string id(int i)
			{
				string text2 = arg(i);
				if (text2.Length == 0)
				{
					throw new CompileError("a setting id can't be empty", at);
				}
				if (!ids.Add(text2))
				{
					throw new CompileError("two settings have the id \"" + text2 + "\": ids must be unique within the mod", at);
				}
				return text2;
			}
			T named<T>(string name2, T fallback)
			{
				return Named(a, name2, fallback);
			}
			string[] namedArray(string text2)
			{
				TypedConstant value = a.NamedArguments.FirstOrDefault<KeyValuePair<string, TypedConstant>>((KeyValuePair<string, TypedConstant> n) => n.Key == text2).Value;
				if (value.Kind != TypedConstantKind.Array || value.IsNull)
				{
					return Array.Empty<string>();
				}
				return value.Values.Select((TypedConstant v) => (v.Value as string) ?? "").ToArray();
			}
		}
		return list;
	}

	private static T Named<T>(AttributeData a, string name, T fallback)
	{
		TypedConstant value = a.NamedArguments.FirstOrDefault<KeyValuePair<string, TypedConstant>>((KeyValuePair<string, TypedConstant> n) => n.Key == name).Value;
		if (!value.IsNull)
		{
			object value2 = value.Value;
			if (value2 is T)
			{
				return (T)value2;
			}
		}
		return fallback;
	}

	private static ModSettingData Slider(AttributeData a, string id, string label, string? description, Location? at)
	{
		double num = Named(a, "Min", 0.0);
		double num2 = Named(a, "Max", 1.0);
		double num3 = Named(a, "Default", 0.0);
		if (num2 <= num)
		{
			throw new CompileError("slider \"" + id + "\": Max must be greater than Min", at);
		}
		if (num3 < num || num3 > num2)
		{
			throw new CompileError("slider \"" + id + "\": Default must be between Min and Max", at);
		}
		return new ModSettingData
		{
			Type = ModSettingType.Slider,
			Id = id,
			Label = label,
			Description = description,
			DefaultValue = num3,
			Min = num,
			Max = num2,
			Step = Named(a, "Step", 0.0),
			ValueWidth = Named(a, "ValueWidth", 0.0),
			Percentage = Named(a, "Percentage", fallback: false)
		};
	}

	private static ModSettingData Select(string id, string label, string? description, string[] options, int selected, Location? at)
	{
		if (options.Length == 0)
		{
			throw new CompileError("select \"" + id + "\" needs at least one option", at);
		}
		if (selected < 0 || selected >= options.Length)
		{
			throw new CompileError($"select \"{id}\": Default is an option index, from 0 to {options.Length - 1}", at);
		}
		return new ModSettingData
		{
			Type = ModSettingType.Select,
			Id = id,
			Label = label,
			Description = description,
			Options = options,
			DefaultOption = selected
		};
	}

	private static string WidgetClass(ModCompiler mod, INamedTypeSymbol? type, Location? at)
	{
		if (type == null || !Symbols.IsSource(type) || type.IsAbstract || !mod.IsSubclass(Symbols.ClassPathAttr(mod.Symbols.NativeBase(type)) ?? "", "/Script/UMG.UserWidget"))
		{
			throw new CompileError("[ModSetting.Widget] needs a widget class of your mod (a class deriving from UUserWidget)", at);
		}
		return $"{mod.Symbols.ModRoot}/{type.Name}.{type.Name}_C";
	}

	public static (float, float, float, float) LinearColour(string hex, Location? at)
	{
		string text = hex.TrimStart('#');
		int length = text.Length;
		bool flag = ((length == 6 || length == 8) ? true : false);
		if (!flag || !uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result))
		{
			throw new CompileError("\"" + hex + "\" is not a hex colour: use \"#RRGGBB\", e.g. \"#FF8800\"", at);
		}
		if (text.Length == 6)
		{
			result = (result << 8) | 0xFF;
		}
		return (linear(result >> 24), linear((result >> 16) & 0xFF), linear((result >> 8) & 0xFF), (float)(result & 0xFF) / 255f);
		static float linear(uint c)
		{
			double num = (double)c / 255.0;
			return (float)((num <= 0.04045) ? (num / 12.92) : Math.Pow((num + 0.055) / 1.055, 2.4));
		}
	}
}
