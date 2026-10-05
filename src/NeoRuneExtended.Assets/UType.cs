using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoRuneExtended.Assets;

public abstract record UType
{
	public sealed record Prim(string Kind, int ByteSize) : UType
	{
		public override string PropertyType => Kind;

		public override int Size => ByteSize;

		public override string ToString()
		{
			return Kind.Replace("Property", "");
		}
	}

	public sealed record Object(string ClassPath) : UType
	{
		public override string PropertyType => "ObjectProperty";

		public override int Size => 8;

		public override string ToString()
		{
			return "Object<" + ClassPath + ">";
		}
	}

	public sealed record Class(string MetaClassPath) : UType
	{
		public override string PropertyType => "ClassProperty";

		public override int Size => 8;

		public override string ToString()
		{
			return "Class<" + MetaClassPath + ">";
		}
	}

	public sealed record SoftObject(string ClassPath) : UType
	{
		public override string PropertyType => "SoftObjectProperty";

		public override int Size => 40;
	}

	public sealed record SoftClass(string MetaClassPath) : UType
	{
		public override string PropertyType => "SoftClassProperty";

		public override int Size => 40;
	}

	public sealed record WeakObject(string ClassPath) : UType
	{
		public override string PropertyType => "WeakObjectProperty";

		public override int Size => 8;
	}

	public sealed record Interface(string InterfaceClassPath) : UType
	{
		public override string PropertyType => "InterfaceProperty";

		public override int Size => 16;
	}

	public sealed record Struct(string StructPath, int StructSize = 0) : UType
	{
		public override string PropertyType => "StructProperty";

		public override int Size => StructSize;

		public override string ToString()
		{
			return "Struct<" + StructPath + ">";
		}
	}

	public sealed record Enum(string EnumPath, UType? Underlying = null) : UType
	{
		public override string PropertyType
		{
			get
			{
				if (!(Underlying == null))
				{
					return "EnumProperty";
				}
				return "ByteProperty";
			}
		}

		public override int Size => Underlying?.Size ?? 1;
	}

	public sealed record Array(UType Inner) : UType
	{
		public override string PropertyType => "ArrayProperty";

		public override int Size => 16;

		public override string ToString()
		{
			return $"Array<{Inner}>";
		}
	}

	public sealed record Set(UType Element) : UType
	{
		public override string PropertyType => "SetProperty";

		public override int Size => 80;
	}

	public sealed record Map(UType Key, UType Value) : UType
	{
		public override string PropertyType => "MapProperty";

		public override int Size => 80;
	}

	public sealed record Delegate(string SignaturePath) : UType
	{
		public override string PropertyType => "DelegateProperty";

		public override int Size => 16;
	}

	public sealed record MulticastDelegate(string SignaturePath) : UType
	{
		public override string PropertyType => "MulticastInlineDelegateProperty";

		public override int Size => 16;
	}

	public string Descriptor
	{
		get
		{
			Prim prim = this as Prim;
			if ((object)prim == null)
			{
				if (!(this is Object obj))
				{
					if (!(this is Class obj2))
					{
						if (!(this is SoftObject softObject))
						{
							if (!(this is SoftClass softClass))
							{
								if (!(this is WeakObject weakObject))
								{
									if (!(this is Interface obj3))
									{
										if (!(this is Struct obj4))
										{
											if (this is Enum obj5)
											{
												if ((object)obj5.Underlying == null)
												{
													return "enum(" + obj5.EnumPath + ")";
												}
												Enum obj6 = obj5;
												return $"enum({obj6.EnumPath},{obj6.Underlying.Descriptor})";
											}
											if (!(this is Delegate obj7))
											{
												if (!(this is MulticastDelegate multicastDelegate))
												{
													if (!(this is Array array))
													{
														if (!(this is Set set))
														{
															if (!(this is Map map))
															{
																throw new NotSupportedException();
															}
															return $"map({map.Key.Descriptor},{map.Value.Descriptor})";
														}
														return "set(" + set.Element.Descriptor + ")";
													}
													return "array(" + array.Inner.Descriptor + ")";
												}
												return "mdelegate(" + multicastDelegate.SignaturePath + ")";
											}
											return "delegate(" + obj7.SignaturePath + ")";
										}
										return (obj4.StructSize > 0) ? $"struct({obj4.StructPath},{obj4.StructSize})" : ("struct(" + obj4.StructPath + ")");
									}
									return "interface(" + obj3.InterfaceClassPath + ")";
								}
								return "weakobject(" + weakObject.ClassPath + ")";
							}
							return "softclass(" + softClass.MetaClassPath + ")";
						}
						return "softobject(" + softObject.ClassPath + ")";
					}
					return "class(" + obj2.MetaClassPath + ")";
				}
				return "object(" + obj.ClassPath + ")";
			}
			return Prims.First<KeyValuePair<string, UType>>((KeyValuePair<string, UType> kv) => kv.Value == prim).Key;
		}
	}

	public abstract string PropertyType { get; }

	public abstract int Size { get; }

	public static readonly UType Bool = new Prim("BoolProperty", 1);

	public static readonly UType Byte = new Prim("ByteProperty", 1);

	public static readonly UType Int = new Prim("IntProperty", 4);

	public static readonly UType Int64 = new Prim("Int64Property", 8);

	public static readonly UType Float = new Prim("FloatProperty", 4);

	public static readonly UType Double = new Prim("DoubleProperty", 8);

	public static readonly UType String = new Prim("StrProperty", 16);

	public static readonly UType Name = new Prim("NameProperty", 8);

	public static readonly UType Text = new Prim("TextProperty", 24);

	public static readonly UType Int8 = new Prim("Int8Property", 1);

	public static readonly UType Int16 = new Prim("Int16Property", 2);

	public static readonly UType UInt16 = new Prim("UInt16Property", 2);

	public static readonly UType UInt32 = new Prim("UInt32Property", 4);

	public static readonly UType UInt64 = new Prim("UInt64Property", 8);

	private static readonly Dictionary<string, UType> Prims = new Dictionary<string, UType>
	{
		["bool"] = Bool,
		["int8"] = Int8,
		["byte"] = Byte,
		["int16"] = Int16,
		["int"] = Int,
		["int64"] = Int64,
		["uint16"] = UInt16,
		["uint32"] = UInt32,
		["uint64"] = UInt64,
		["float"] = Float,
		["double"] = Double,
		["string"] = String,
		["name"] = Name,
		["text"] = Text
	};

	public static UType Parse(string descriptor)
	{
		int pos = 0;
		UType result = ParseAt(descriptor, ref pos);
		if (pos != descriptor.Length)
		{
			throw new FormatException("trailing text in type '" + descriptor + "'");
		}
		return result;
	}

	private static UType ParseAt(string s, ref int pos)
	{
		int num = pos;
		while (pos < s.Length && char.IsLetterOrDigit(s[pos]))
		{
			pos++;
		}
		int num2 = num;
		string text = s.Substring(num2, pos - num2);
		if (pos >= s.Length || s[pos] != '(')
		{
			if (!Prims.TryGetValue(text, out UType value))
			{
				throw new FormatException("unknown type '" + text + "'");
			}
			return value;
		}
		pos++;
		List<string> list = new List<string>();
		List<UType> list2 = new List<UType>();
		bool flag;
		switch (text)
		{
		case "array":
		case "set":
		case "map":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		bool flag2 = flag;
		do
		{
			if (flag2)
			{
				list2.Add(ParseAt(s, ref pos));
			}
			else
			{
				int num3 = pos;
				while (pos < s.Length && s[pos] != ',' && s[pos] != ')')
				{
					pos++;
				}
				num2 = num3;
				list.Add(s.Substring(num2, pos - num2));
			}
			if (pos >= s.Length)
			{
				throw new FormatException("unterminated type '" + s + "'");
			}
		}
		while (s[pos++] != ')');
		return text switch
		{
			"object" => new Object(list[0]), 
			"class" => new Class(list[0]), 
			"softobject" => new SoftObject(list[0]), 
			"softclass" => new SoftClass(list[0]), 
			"weakobject" => new WeakObject(list[0]), 
			"interface" => new Interface(list[0]), 
			"struct" => new Struct(list[0], (list.Count > 1) ? int.Parse(list[1]) : 0), 
			"enum" => new Enum(list[0], (list.Count > 1) ? Prims[list[1]] : null), 
			"delegate" => new Delegate(list[0]), 
			"mdelegate" => new MulticastDelegate(list[0]), 
			"array" => new Array(list2[0]), 
			"set" => new Set(list2[0]), 
			"map" => new Map(list2[0], list2[1]), 
			_ => throw new FormatException("unknown type '" + text + "'"), 
		};
	}
}
