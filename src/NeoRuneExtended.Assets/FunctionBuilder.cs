using System;
using System.Collections.Generic;
using System.Linq;
using UAssetAPI.ExportTypes;
using UAssetAPI.FieldTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;

namespace NeoRuneExtended.Assets;

public sealed class FunctionBuilder
{
	private readonly FunctionExport export;

	private readonly List<FProperty> parameters = new List<FProperty>();

	private readonly List<FProperty> locals = new List<FProperty>();

	private readonly Dictionary<string, UType> types = new Dictionary<string, UType>();

	public BlueprintBuilder Owner { get; }

	public string Name { get; }

	public FPackageIndex Index { get; }

	public ScriptBuilder Script { get; }

	public string? ReturnValueName { get; private set; }

	internal FunctionBuilder(BlueprintBuilder owner, string name, EFunctionFlags flags, string? overridesPath)
	{
		Owner = owner;
		Name = name;
		PackageBuilder package = owner.Package;
		FPackageIndex fPackageIndex = ((overridesPath != null) ? package.ImportFunction(overridesPath) : FPackageIndex.FromRawIndex(0));
		export = new FunctionExport
		{
			ObjectName = package.Name(name),
			ClassIndex = package.ImportClass("/Script/CoreUObject.Function"),
			SuperIndex = fPackageIndex,
			TemplateIndex = package.ImportDefaultObject("/Script/CoreUObject.Function"),
			OuterIndex = owner.ClassIndex,
			ObjectFlags = EObjectFlags.RF_Public,
			Data = new List<PropertyData>(),
			FunctionFlags = flags,
			SuperStruct = fPackageIndex,
			Children = Array.Empty<FPackageIndex>(),
			Field = new UField(),
			Extras = new byte[8]
		};
		Index = package.AddExport(export);
		Script = new ScriptBuilder(this);
	}

	public bool HasLocal(string name)
	{
		return types.ContainsKey(name);
	}

	public UType TypeOf(string name)
	{
		return types[name];
	}

	public void AddParameter(string name, UType type, bool isOut = false, bool isReturn = false, bool isConstRef = false)
	{
		EPropertyFlags ePropertyFlags = EPropertyFlags.CPF_BlueprintVisible | EPropertyFlags.CPF_Parm;
		if (isOut | isReturn)
		{
			ePropertyFlags |= EPropertyFlags.CPF_OutParm;
		}
		if (isReturn)
		{
			ePropertyFlags |= EPropertyFlags.CPF_ReturnParm;
		}
		if (isConstRef)
		{
			ePropertyFlags |= EPropertyFlags.CPF_ConstParm | EPropertyFlags.CPF_OutParm | EPropertyFlags.CPF_ReferenceParm;
		}
		parameters.Add(Owner.Package.Property(name, type, ePropertyFlags));
		types[name] = type;
		if (isReturn)
		{
			ReturnValueName = name;
		}
		if (isOut | isReturn)
		{
			export.FunctionFlags |= EFunctionFlags.FUNC_HasOutParms;
		}
	}

	public void AddLocal(string name, UType type)
	{
		if (types.ContainsKey(name))
		{
			throw new InvalidOperationException("Duplicate local " + name + " in " + Name);
		}
		locals.Add(Owner.Package.Property(name, type, EPropertyFlags.CPF_None));
		types[name] = type;
	}

	internal void Finish()
	{
		FProperty[] array = parameters.Concat(locals).ToArray();
		export.LoadedProperties = array;
		export.ScriptBytecode = Script.Build();
		export.SerializationBeforeSerializationDependencies = new List<FPackageIndex>();
		if (export.SuperIndex.Index != 0)
		{
			export.SerializationBeforeSerializationDependencies.Add(export.SuperIndex);
		}
		List<FPackageIndex> list = new List<FPackageIndex> { Index };
		FProperty[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			BlueprintBuilder.AddTypeDeps(array2[i], list);
		}
		foreach (FPackageIndex r in Script.References)
		{
			if (!list.Any((FPackageIndex d) => d.Index == r.Index))
			{
				list.Add(r);
			}
		}
		export.CreateBeforeSerializationDependencies = list;
		export.SerializationBeforeCreateDependencies = new List<FPackageIndex>();
		export.CreateBeforeCreateDependencies = new List<FPackageIndex> { Owner.ClassIndex };
		if (export.SuperIndex.Index != 0)
		{
			export.CreateBeforeCreateDependencies.Add(export.SuperIndex);
		}
	}
}
