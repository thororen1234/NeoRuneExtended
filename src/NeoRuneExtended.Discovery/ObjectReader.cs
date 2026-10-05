using System;
using System.Collections.Generic;
using System.Text;

namespace NeoRuneExtended.Discovery;

public sealed class ObjectReader
{
	public const int ObjClass = 16;

	public const int ObjName = 24;

	public const int ObjOuter = 32;

	public const int FieldNext = 40;

	public const int StructSuper = 64;

	public const int StructChildren = 72;

	public const int StructChildProperties = 80;

	public const int StructSize = 88;

	public const int FFieldClass = 8;

	public const int FFieldNext = 24;

	public const int FFieldName = 32;

	private readonly GameProcess p;

	private readonly ulong nameBlocks;

	private readonly ulong objectArray;

	private readonly Dictionary<uint, string> names = new Dictionary<uint, string>();

	private readonly Dictionary<ulong, string> objectNames = new Dictionary<ulong, string>();

	private readonly Dictionary<ulong, string> classNameCache = new Dictionary<ulong, string>();

	public List<ulong> Objects { get; } = new List<ulong>();

	public HashSet<ulong> ObjectSet { get; } = new HashSet<ulong>();

	public ObjectReader(GameProcess process)
	{
		p = process;
		(nameBlocks, objectArray) = Locate();
		LoadObjects();
	}

	private (ulong Blocks, ulong Guo) Locate()
	{
		byte[] array = p.Read(p.Base, 4096);
		int num = BitConverter.ToInt32(array, 60);
		int num2 = BitConverter.ToUInt16(array, num + 6);
		int num3 = BitConverter.ToUInt16(array, num + 20);
		ulong num4 = 0uL;
		ulong num5 = 0uL;
		for (int i = 0; i < num2; i++)
		{
			int num6 = num + 24 + num3 + 40 * i;
			if (Encoding.ASCII.GetString(array, num6, 8).TrimEnd('\0') == ".data")
			{
				num4 = p.Base + BitConverter.ToUInt32(array, num6 + 12);
				num5 = num4 + BitConverter.ToUInt32(array, num6 + 8);
			}
		}
		ulong num7 = 0uL;
		ulong num8 = 0uL;
		for (ulong num9 = num4; num9 < num5; num9 += 1048576)
		{
			if (num7 != 0L && num8 != 0L)
			{
				break;
			}
			byte[] array2 = p.Read(num9, (int)Math.Min(1048576uL, num5 - num9));
			if (array2 == null)
			{
				continue;
			}
			for (int j = 8; j < array2.Length - 48; j += 8)
			{
				ulong num10 = BitConverter.ToUInt64(array2, j);
				if (num10 < 65536 || num10 > 140737488355327L)
				{
					continue;
				}
				if (num7 == 0L)
				{
					uint num11 = BitConverter.ToUInt32(array2, j - 8);
					uint num12 = BitConverter.ToUInt32(array2, j - 4);
					if (num11 != 0 && num11 < 4096 && num12 < 262144)
					{
						byte[] array3 = p.Read(num10, 8);
						if (array3 != null && Encoding.ASCII.GetString(array3, 2, 4) == "None")
						{
							num7 = num9 + (ulong)j;
						}
					}
				}
				if (num8 != 0L || j < 16)
				{
					continue;
				}
				int num13 = BitConverter.ToInt32(array2, j + 16);
				int num14 = BitConverter.ToInt32(array2, j + 20);
				int num15 = BitConverter.ToInt32(array2, j + 24);
				int num16 = BitConverter.ToInt32(array2, j + 28);
				if (num13 > 100000 && num13 < 10000000 && num14 > 1000 && num14 <= num13 && num16 == (num14 + 65535) / 65536 && num16 > 0 && num16 <= num15)
				{
					ulong num17 = p.U64(num10);
					ulong num18 = ((num17 != 0L) ? p.U64(num17) : 0);
					if (num18 != 0L && p.Read(num18, 8) != null)
					{
						num8 = (ulong)((long)num9 + (long)j - 16);
					}
				}
			}
		}
		if (num7 == 0L || num8 == 0L)
		{
			throw new InvalidOperationException("Could not locate FNamePool / GUObjectArray; the game may still be loading.");
		}
		return (Blocks: num7, Guo: num8);
	}

	private void LoadObjects()
	{
		byte[]? value = p.Read(objectArray + 16, 32);
		ulong num = BitConverter.ToUInt64(value, 0);
		int num2 = BitConverter.ToInt32(value, 20);
		int num3 = BitConverter.ToInt32(value, 28);
		for (int i = 0; i < num3; i++)
		{
			ulong addr = p.U64(num + (ulong)(8L * (long)i));
			int num4 = Math.Min(65536, num2 - i * 65536);
			byte[] array = p.Read(addr, 24 * num4);
			if (array == null)
			{
				continue;
			}
			for (int j = 0; j < array.Length / 24; j++)
			{
				ulong num5 = BitConverter.ToUInt64(array, 24 * j);
				if (num5 != 0L)
				{
					Objects.Add(num5);
					ObjectSet.Add(num5);
				}
			}
		}
	}

	public string NameEntry(uint index)
	{
		if (names.TryGetValue(index, out string value))
		{
			return value;
		}
		ulong num = p.U64(nameBlocks + 8 * (index >> 16));
		if (num == 0L)
		{
			return "?";
		}
		ulong num2 = num + 2 * (index & 0xFFFF);
		ushort num3 = p.U16(num2);
		int num4 = num3 >> 6;
		bool flag = (num3 & 1) != 0;
		byte[] bytes = p.Read(num2 + 2, num4 * ((!flag) ? 1 : 2)) ?? Array.Empty<byte>();
		value = (flag ? Encoding.Unicode.GetString(bytes) : Encoding.Latin1.GetString(bytes));
		names[index] = value;
		return value;
	}

	public string FName(ulong addr)
	{
		byte[] array = p.Read(addr, 8);
		if (array == null)
		{
			return "?";
		}
		uint index = BitConverter.ToUInt32(array, 0);
		uint num = BitConverter.ToUInt32(array, 4);
		string text = NameEntry(index);
		if (num != 0)
		{
			return $"{text}_{num - 1}";
		}
		return text;
	}

	public ulong Class(ulong o)
	{
		return p.U64(o + 16);
	}

	public ulong Outer(ulong o)
	{
		return p.U64(o + 32);
	}

	public string Name(ulong o)
	{
		if (!objectNames.TryGetValue(o, out string value))
		{
			return objectNames[o] = FName(o + 24);
		}
		return value;
	}

	public string ClassName(ulong o)
	{
		ulong num = Class(o);
		if (!classNameCache.TryGetValue(num, out string value))
		{
			return classNameCache[num] = Name(num);
		}
		return value;
	}

	public string PathName(ulong o)
	{
		List<string> list = new List<string>();
		for (ulong num = o; num != 0L; num = Outer(num))
		{
			list.Add(Name(num));
		}
		list.Reverse();
		if (list.Count == 1)
		{
			return list[0];
		}
		StringBuilder stringBuilder = new StringBuilder(list[0]).Append('.').Append(list[1]);
		for (int i = 2; i < list.Count; i++)
		{
			stringBuilder.Append(':').Append(list[i]);
		}
		return stringBuilder.ToString();
	}

	public string PackageName(ulong o)
	{
		while (Outer(o) != 0L)
		{
			o = Outer(o);
		}
		return Name(o);
	}
}
