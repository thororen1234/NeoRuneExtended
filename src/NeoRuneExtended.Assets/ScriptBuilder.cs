using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UAssetAPI;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.UnrealTypes;

namespace NeoRuneExtended.Assets;

public sealed class ScriptBuilder
{
	private readonly FunctionBuilder fn;

	private readonly List<object> items = new List<object>();

	private readonly Dictionary<KismetExpression, Label> jumps = new Dictionary<KismetExpression, Label>();

	private readonly HashSet<int> references = new HashSet<int>();

	private int labelCount;

	public static readonly KismetPropertyPointer NullPointer = new KismetPropertyPointer(new FFieldPath(Array.Empty<FName>(), FPackageIndex.FromRawIndex(0)));

	private PackageBuilder P => fn.Owner.Package;

	private UAsset Asset => P.Asset;

	public IEnumerable<FPackageIndex> References => references.Select(FPackageIndex.FromRawIndex);

	internal ScriptBuilder(FunctionBuilder fn)
	{
		this.fn = fn;
	}

	public Label NewLabel(string hint = "L")
	{
		return new Label($"{hint}{labelCount++}");
	}

	public void Mark(Label label)
	{
		items.Add(label);
	}

	public void Emit(KismetExpression statement)
	{
		items.Add(statement);
	}

	public void Jump(Label target)
	{
		EX_Jump eX_Jump = new EX_Jump();
		jumps[eX_Jump] = target;
		items.Add(eX_Jump);
	}

	public void JumpIfNot(KismetExpression condition, Label target)
	{
		EX_JumpIfNot eX_JumpIfNot = new EX_JumpIfNot
		{
			BooleanExpression = condition
		};
		jumps[eX_JumpIfNot] = target;
		items.Add(eX_JumpIfNot);
	}

	public FPackageIndex Ref(FPackageIndex index)
	{
		if (index.Index != 0)
		{
			references.Add(index.Index);
		}
		return index;
	}

	public int Measure(KismetExpression e)
	{
		using MemoryStream stream = new MemoryStream();
		using AssetBinaryWriter writer = new AssetBinaryWriter(stream, Asset);
		return ExpressionSerializer.WriteExpression(e, writer);
	}

	internal KismetExpression[] Build()
	{
		List<KismetExpression> list = new List<KismetExpression>();
		uint num = 0u;
		foreach (object item2 in items)
		{
			if (item2 is Label label)
			{
				label.Offset = num;
				continue;
			}
			KismetExpression kismetExpression = (KismetExpression)item2;
			list.Add(kismetExpression);
			num += (uint)Measure(kismetExpression);
		}
		EX_Return item = new EX_Return
		{
			ReturnExpression = ReturnExpression()
		};
		list.Add(item);
		list.Add(new EX_EndOfScript());
		foreach (var (kismetExpression3, label3) in jumps)
		{
			if (!label3.Offset.HasValue)
			{
				throw new InvalidOperationException($"Label {label3} was never marked in {fn.Name}");
			}
			if (!(kismetExpression3 is EX_Jump eX_Jump))
			{
				if (kismetExpression3 is EX_JumpIfNot eX_JumpIfNot)
				{
					eX_JumpIfNot.CodeOffset = label3.Offset.Value;
				}
			}
			else
			{
				eX_Jump.CodeOffset = label3.Offset.Value;
			}
		}
		return list.ToArray();
	}

	private KismetExpression ReturnExpression()
	{
		string returnValueName = fn.ReturnValueName;
		if (returnValueName == null)
		{
			return new EX_Nothing();
		}
		return new EX_LocalOutVariable
		{
			Variable = Pointer(returnValueName, fn.Index)
		};
	}

	public KismetPropertyPointer Pointer(string propertyName, FPackageIndex owner)
	{
		return new KismetPropertyPointer(new FFieldPath(new FName[1] { P.Name(PackageBuilder.PropertyName(propertyName)) }, owner));
	}

	public EX_VariableBase Local(string name, bool outParam = false)
	{
		if (!outParam)
		{
			return new EX_LocalVariable
			{
				Variable = Pointer(name, fn.Index)
			};
		}
		return new EX_LocalOutVariable
		{
			Variable = Pointer(name, fn.Index)
		};
	}

	public EX_InstanceVariable Member(string name, FPackageIndex owner)
	{
		return new EX_InstanceVariable
		{
			Variable = Pointer(name, Ref(owner))
		};
	}

	public EX_Context Context(KismetExpression target, KismetExpression inner, KismetPropertyPointer? rvalue = null, bool failSilent = false)
	{
		EX_Context obj = (failSilent ? new EX_Context_FailSilent() : new EX_Context());
		obj.ObjectExpression = target;
		obj.ContextExpression = inner;
		obj.RValuePointer = rvalue ?? NullPointer;
		obj.Offset = (uint)Measure(inner);
		return obj;
	}

	public EX_CallMath CallMath(FPackageIndex function, params KismetExpression[] args)
	{
		return new EX_CallMath
		{
			StackNode = Ref(function),
			Parameters = args
		};
	}

	public EX_FinalFunction CallFinal(FPackageIndex function, params KismetExpression[] args)
	{
		return new EX_FinalFunction
		{
			StackNode = Ref(function),
			Parameters = args
		};
	}

	public EX_VirtualFunction CallVirtual(string name, params KismetExpression[] args)
	{
		return new EX_VirtualFunction
		{
			VirtualFunctionName = P.Name(name),
			Parameters = args
		};
	}

	public EX_LocalFinalFunction CallLocalFinal(FPackageIndex function, params KismetExpression[] args)
	{
		return new EX_LocalFinalFunction
		{
			StackNode = function,
			Parameters = args
		};
	}

	public EX_LocalVirtualFunction CallLocalVirtual(string name, params KismetExpression[] args)
	{
		return new EX_LocalVirtualFunction
		{
			VirtualFunctionName = P.Name(name),
			Parameters = args
		};
	}

	public KismetExpression Let(UType type, KismetExpression destination, KismetExpression value, KismetPropertyPointer destinationProperty)
	{
		if (type is UType.Prim prim)
		{
			if (prim.Kind == "BoolProperty")
			{
				return new EX_LetBool
				{
					VariableExpression = destination,
					AssignmentExpression = value
				};
			}
		}
		else
		{
			if (type is UType.Object || type is UType.Class)
			{
				return new EX_LetObj
				{
					VariableExpression = destination,
					AssignmentExpression = value
				};
			}
			if (type is UType.WeakObject)
			{
				return new EX_LetWeakObjPtr
				{
					VariableExpression = destination,
					AssignmentExpression = value
				};
			}
			if (type is UType.Delegate)
			{
				return new EX_LetDelegate
				{
					VariableExpression = destination,
					AssignmentExpression = value
				};
			}
			if (type is UType.MulticastDelegate)
			{
				return new EX_LetMulticastDelegate
				{
					VariableExpression = destination,
					AssignmentExpression = value
				};
			}
		}
		return new EX_Let
		{
			Value = destinationProperty,
			Variable = destination,
			Expression = value
		};
	}

	public static KismetExpression Int(int v)
	{
		return new EX_IntConst
		{
			Value = v
		};
	}

	public static KismetExpression Int64(long v)
	{
		return new EX_Int64Const
		{
			Value = v
		};
	}

	public static KismetExpression Byte(byte v)
	{
		return new EX_ByteConst
		{
			Value = v
		};
	}

	public static KismetExpression Float(float v)
	{
		return new EX_FloatConst
		{
			Value = v
		};
	}

	public static KismetExpression Double(double v)
	{
		return new EX_DoubleConst
		{
			Value = v
		};
	}

	public static KismetExpression Bool(bool v)
	{
		if (!v)
		{
			return new EX_False();
		}
		return new EX_True();
	}

	public static KismetExpression Str(string v)
	{
		if (!v.All((char c) => c < '\u0080'))
		{
			return new EX_UnicodeStringConst
			{
				Value = v
			};
		}
		return new EX_StringConst
		{
			Value = v
		};
	}

	public KismetExpression NameConst(string v)
	{
		return new EX_NameConst
		{
			Value = P.Name(v)
		};
	}

	public KismetExpression Object(FPackageIndex obj)
	{
		return new EX_ObjectConst
		{
			Value = Ref(obj)
		};
	}

	public static KismetExpression Self()
	{
		return new EX_Self();
	}

	public static KismetExpression NoObject()
	{
		return new EX_NoObject();
	}

	public static KismetExpression Nothing()
	{
		return new EX_Nothing();
	}
}
