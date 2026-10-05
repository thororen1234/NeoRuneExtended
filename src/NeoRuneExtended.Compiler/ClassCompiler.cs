using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using NeoRuneExtended.Assets;
using UAssetAPI.UnrealTypes;

namespace NeoRuneExtended.Compiler;

internal sealed class ClassCompiler
{
	private readonly List<INamedTypeSymbol> chain = new List<INamedTypeSymbol>();

	private readonly Dictionary<ISymbol, SelfMember> fields = new Dictionary<ISymbol, SelfMember>(SymbolEqualityComparer.Default);

	private readonly Dictionary<IMethodSymbol, MethodPlan> plans = new Dictionary<IMethodSymbol, MethodPlan>(SymbolEqualityComparer.Default);

	private readonly Queue<MethodPlan> pending = new Queue<MethodPlan>();

	private readonly HashSet<string> functionNames = new HashSet<string>();

	private const uint OwnFunctionFlags = 67239937u;

	private const uint InterfaceFunctionFlags = 201457664u;

	private const uint EventFlagMask = 1544423424u;

	private List<IOperation> initializers = new List<IOperation>();

	private MethodPlan? beginPlay;

	private IMethodSymbol? startup;

	public ModCompiler Mod { get; }

	public Symbols Symbols => Mod.Symbols;

	public INamedTypeSymbol Type { get; }

	public BlueprintBuilder Blueprint { get; }

	public string ClassPath { get; }

	public bool IsActor => IsSubclass(Blueprint.ParentClassPath, "/Script/Engine.Actor");

	public bool IsFunctionLibrary => IsSubclass(Blueprint.ParentClassPath, "/Script/Engine.BlueprintFunctionLibrary");

	public ClassCompiler(ModCompiler mod, INamedTypeSymbol type)
	{
		Mod = mod;
		Type = type;
		INamedTypeSymbol namedTypeSymbol = type;
		while (namedTypeSymbol != null && NeoRuneExtended.Compiler.Symbols.IsSource(namedTypeSymbol))
		{
			chain.Add(namedTypeSymbol);
			namedTypeSymbol = namedTypeSymbol.BaseType;
		}
		string parentClassPath = NeoRuneExtended.Compiler.Symbols.ClassPathAttr(Symbols.NativeBase(type) ?? throw new CompileError(type.Name + " must derive from a game class (e.g. AActor)", type.Locations.FirstOrDefault()));
		string package = Symbols.ModPackage(type);
		Blueprint = new BlueprintBuilder(package, NeoRuneExtended.Compiler.Symbols.AssetName(package), parentClassPath);
		ClassPath = Blueprint.ClassPath;
	}

	public bool IsSubclass(string cls, string ancestor)
	{
		return Mod.IsSubclass(cls, ancestor);
	}

	public UFunctionInfo? Library(string className, string name)
	{
		return Mod.Library(className, name);
	}

	public FPackageIndex ImportOwnerOf(ISymbol member)
	{
		return Blueprint.Package.ImportClass(Symbols.ClassPath(member.ContainingType));
	}

	public SelfMember? Field(ISymbol symbol)
	{
		if (fields.TryGetValue(symbol, out SelfMember value))
		{
			return value;
		}
		if (symbol is IFieldSymbol { AssociatedSymbol: IPropertySymbol associatedSymbol } && fields.TryGetValue(associatedSymbol, out value))
		{
			return value;
		}
		if (!NeoRuneExtended.Compiler.Symbols.IsSource(symbol.ContainingType) || symbol.IsStatic || chain.Contains(symbol.ContainingType, SymbolEqualityComparer.Default))
		{
			return null;
		}
		if (symbol is IPropertySymbol p && !IsAutoProperty(p))
		{
			return null;
		}
		UType uType = VariableType(symbol);
		if (!(uType == null))
		{
			return new SelfMember(VariableName(symbol), uType, ImportOwnerOf(symbol));
		}
		return null;
	}

	private UType? VariableType(ISymbol member)
	{
		if (!(member is IFieldSymbol fieldSymbol))
		{
			if (!(member is IPropertySymbol propertySymbol))
			{
				if (member is IEventSymbol eventSymbol)
				{
					string text = NeoRuneExtended.Compiler.Symbols.DelegatePath(eventSymbol.Type);
					return (text != null) ? new UType.MulticastDelegate(text) : null;
				}
				return null;
			}
			return Symbols.Map(propertySymbol.Type);
		}
		return Symbols.Map(fieldSymbol.Type);
	}

	private static bool IsFieldLikeEvent(IEventSymbol e)
	{
		return e.AddMethod?.IsImplicitlyDeclared ?? false;
	}

	private static string VariableName(ISymbol s)
	{
		return (s.GetAttributes().FirstOrDefault((AttributeData a) => a.AttributeClass?.Name == "UNameAttribute")?.ConstructorArguments[0].Value as string) ?? s.Name;
	}

	public void Declare()
	{
		foreach (INamedTypeSymbol item in chain)
		{
			foreach (ISymbol member in item.GetMembers())
			{
				if (!member.IsStatic && !member.IsImplicitlyDeclared && (member is IFieldSymbol { IsConst: false, AssociatedSymbol: null } || ((member is IPropertySymbol p) ? IsAutoProperty(p) : (member is IEventSymbol e && IsFieldLikeEvent(e)))))
				{
					UType type = VariableType(member) ?? throw new CompileError((member is IEventSymbol) ? ("event '" + member.Name + "' needs a game event type (e.g. OnButtonClickedEvent); your own delegate types aren't supported") : ("type of '" + member.Name + "' has no Unreal equivalent"), member.Locations.FirstOrDefault());
					string name = VariableName(member);
					Blueprint.AddVariable(name, type);
					fields[member] = new SelfMember(name, type, Blueprint.ClassIndex);
				}
			}
		}
		List<IMethodSymbol> list = (from methodSymbol in chain.SelectMany((INamedTypeSymbol t) => t.GetMembers().OfType<IMethodSymbol>()).Where(delegate(IMethodSymbol methodSymbol)
			{
				MethodKind methodKind = methodSymbol.MethodKind;
				bool flag = ((methodKind == MethodKind.ExplicitInterfaceImplementation || methodKind == MethodKind.Ordinary) ? true : false);
				return flag && !methodSymbol.IsStatic && !methodSymbol.IsAbstract;
			})
			orderby (!methodSymbol.IsOverride && InterfaceMember(methodSymbol) == null) ? 1 : 0
			select methodSymbol).ToList();
		foreach (IMethodSymbol m in list)
		{
			if (!list.Any((IMethodSymbol o) => o.IsOverride && SymbolEqualityComparer.Default.Equals(o.OverriddenMethod, m)))
			{
				Plan(m);
			}
		}
		DeclareInterfaces();
		if (IsFunctionLibrary)
		{
			foreach (IMethodSymbol item in from m in Type.GetMembers().OfType<IMethodSymbol>()
				where m.MethodKind == MethodKind.Ordinary && m.IsStatic && m.DeclaredAccessibility == Accessibility.Public
				select m)
			{
				PlanLibraryFunction(item);
			}
		}
		if (IsActor && plans.Values.Any((MethodPlan methodPlan) => methodPlan.Name == "ReceiveTick"))
		{
			Blueprint.SetDefaultStructBool("PrimaryActorTick", "/Script/Engine.ActorTickFunction", "bCanEverTick", value: true);
		}
		initializers = (from i in chain.SelectMany(FieldInitializers)
			where !IsDefaultInitializer(i)
			select i).ToList();
		beginPlay = plans.Values.FirstOrDefault((MethodPlan methodPlan) => methodPlan.Name == "ReceiveBeginPlay");
		if (initializers.Count > 0 && !IsActor)
		{
			throw new CompileError("field initializers in " + Type.Name + " are only supported on actor classes", initializers[0].Syntax.GetLocation());
		}
		if (Type.Name == "ModActor" && IsActor)
		{
			startup = Mod.Compilation.GetTypeByMetadataName("NeoRune.NeoRuneWatermark")?.GetMembers("Show").OfType<IMethodSymbol>().FirstOrDefault();
		}
		if ((initializers.Count > 0 || startup != null) && beginPlay == null)
		{
			FunctionBuilder fn = Blueprint.AddFunction("ReceiveBeginPlay", EFunctionFlags.FUNC_Event | EFunctionFlags.FUNC_Protected | EFunctionFlags.FUNC_BlueprintEvent, "/Script/Engine.Actor:ReceiveBeginPlay");
			functionNames.Add("ReceiveBeginPlay");
			List<IOperation> inits = initializers;
			Mod.Run(Type, delegate
			{
				new FunctionCompiler(this, new MethodPlan
				{
					Method = null,
					Name = "ReceiveBeginPlay",
					Builder = fn
				}).CompileBody(null, inits, startup);
			});
		}
	}

	private void DeclareInterfaces()
	{
		foreach (INamedTypeSymbol allInterface in Type.AllInterfaces)
		{
			string text = NeoRuneExtended.Compiler.Symbols.ClassPathAttr(allInterface);
			if (text == null)
			{
				continue;
			}
			Blueprint.AddInterface(text);
			foreach (IMethodSymbol item in from m in allInterface.GetMembers().OfType<IMethodSymbol>()
				where m.MethodKind == MethodKind.Ordinary
				select m)
			{
				if (!(Type.FindImplementationForInterfaceMember(item) is IMethodSymbol key) || !plans.ContainsKey(key))
				{
					MethodPlan stub = new MethodPlan
					{
						Method = item,
						Name = item.Name,
						Builder = Blueprint.AddFunction(item.Name, EFunctionFlags.FUNC_Public | EFunctionFlags.FUNC_BlueprintCallable | EFunctionFlags.FUNC_BlueprintEvent)
					};
					functionNames.Add(item.Name);
					foreach (IParameterSymbol parameter in item.Parameters)
					{
						stub.Builder.AddParameter(parameter.Name, Symbols.Map(parameter.Type) ?? throw new CompileError($"parameter type '{parameter.Type}' has no Unreal equivalent", parameter.Locations.FirstOrDefault()));
					}
					Mod.Run(Type, delegate
					{
						new FunctionCompiler(this, stub).CompileBody(null);
					});
				}
			}
		}
	}

	private IMethodSymbol? InterfaceMember(IMethodSymbol m)
	{
		return Type.AllInterfaces.Where((INamedTypeSymbol i) => NeoRuneExtended.Compiler.Symbols.ClassPathAttr(i) != null).SelectMany((INamedTypeSymbol i) => i.GetMembers().OfType<IMethodSymbol>()).FirstOrDefault((IMethodSymbol member) => SymbolEqualityComparer.Default.Equals(Type.FindImplementationForInterfaceMember(member), m));
	}

	public bool Drain()
	{
		if (pending.Count == 0)
		{
			return false;
		}
		while (pending.Count > 0)
		{
			MethodPlan plan = pending.Dequeue();
			bool isBeginPlay = plan == beginPlay;
			Mod.Run(Type, delegate
			{
				new FunctionCompiler(this, plan).CompileBody(Body(plan.Method), isBeginPlay ? initializers : null, isBeginPlay ? startup : null);
			});
		}
		return true;
	}

	private IEnumerable<IOperation> FieldInitializers(INamedTypeSymbol t)
	{
		foreach (SyntaxReference declaringSyntaxReference in t.DeclaringSyntaxReferences)
		{
			SemanticModel model = Mod.Compilation.GetSemanticModel(declaringSyntaxReference.SyntaxTree);
			foreach (SyntaxNode node in declaringSyntaxReference.GetSyntax().DescendantNodes())
			{
				if (node is EqualsValueClauseSyntax node2)
				{
					SyntaxNode parent = node.Parent;
					if (parent is VariableDeclaratorSyntax)
					{
						SyntaxNode parent2 = parent.Parent;
						if (parent2 != null && parent2.Parent is FieldDeclarationSyntax fieldDeclarationSyntax && !fieldDeclarationSyntax.Modifiers.Any(delegate(SyntaxToken m)
						{
							string text = m.Text;
							return (text == "const" || text == "static") ? true : false;
						}))
						{
							IOperation operation = model.GetOperation(node2);
							if (operation != null)
							{
								yield return operation;
							}
						}
					}
				}
				if (node is EqualsValueClauseSyntax node3 && node.Parent is PropertyDeclarationSyntax propertyDeclarationSyntax && !propertyDeclarationSyntax.Modifiers.Any((SyntaxToken m) => m.Text == "static"))
				{
					IOperation operation2 = model.GetOperation(node3);
					if (operation2 != null)
					{
						yield return operation2;
					}
				}
			}
		}
	}

	private static bool IsDefaultInitializer(IOperation init)
	{
		IOperation operation = ((init is IFieldInitializerOperation fieldInitializerOperation) ? fieldInitializerOperation.Value : ((!(init is IPropertyInitializerOperation propertyInitializerOperation)) ? null : propertyInitializerOperation.Value));
		IOperation operation2;
		for (operation2 = operation; operation2 is IConversionOperation conversionOperation; operation2 = conversionOperation.Operand)
		{
		}
		if (operation2 == null)
		{
			return false;
		}
		if (operation2 is IDefaultValueOperation)
		{
			return true;
		}
		Optional<object> constantValue = operation2.ConstantValue;
		if (constantValue.HasValue)
		{
			object value = constantValue.Value;
			if ((value != null && (!(value is bool) || (bool)value)) || 1 == 0)
			{
				if (constantValue.Value is IConvertible convertible && !(convertible is string))
				{
					return Convert.ToDouble(convertible) == 0.0;
				}
				return false;
			}
			return true;
		}
		if (operation2 is IObjectCreationOperation objectCreationOperation && objectCreationOperation.Arguments.Length == 0 && (objectCreationOperation.Initializer == null || objectCreationOperation.Initializer.Initializers.Length == 0))
		{
			if (objectCreationOperation.Type is INamedTypeSymbol { IsGenericType: not false } namedTypeSymbol)
			{
				return namedTypeSymbol.ContainingNamespace.ToDisplayString() == "System.Collections.Generic";
			}
			return false;
		}
		if (operation2 is ICollectionExpressionOperation collectionExpressionOperation && collectionExpressionOperation.Elements.Length == 0)
		{
			return true;
		}
		return false;
	}

	private static bool IsAutoProperty(IPropertySymbol p)
	{
		return p.ContainingType.GetMembers().OfType<IFieldSymbol>().Any((IFieldSymbol f) => SymbolEqualityComparer.Default.Equals(f.AssociatedSymbol, p));
	}

	private IOperation Body(IMethodSymbol m)
	{
		SyntaxNode syntaxNode = m.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() ?? throw new CompileError("no source for " + m.ToDisplayString(), m.Locations.FirstOrDefault());
		IOperation operation = Mod.Compilation.GetSemanticModel(syntaxNode.SyntaxTree).GetOperation(syntaxNode);
		IOperation operation2 = operation;
		if (!(operation2 is IBlockOperation result) || !(syntaxNode is ArrowExpressionClauseSyntax))
		{
			if (!(operation2 is IAnonymousFunctionOperation anonymousFunctionOperation))
			{
				if (operation2 is ILocalFunctionOperation localFunctionOperation)
				{
					IOperation body = localFunctionOperation.Body;
					return body ?? localFunctionOperation.IgnoredBody;
				}
				IMethodBodyOperation methodBodyOperation = (operation as IMethodBodyOperation) ?? throw new CompileError("cannot compile " + m.Name, m.Locations.FirstOrDefault());
				operation2 = methodBodyOperation.BlockBody;
				return operation2 ?? methodBodyOperation.ExpressionBody ?? throw new CompileError(m.Name + " has no body (auto-accessors mixed with bodies, like 'get; set { }', are not supported)", m.Locations.FirstOrDefault());
			}
			return anonymousFunctionOperation.Body;
		}
		return result;
	}

	public MethodPlan Callable(IMethodSymbol m, IOperation at)
	{
		m = m.OriginalDefinition;
		// A virtual call runs the override: a base class's code calling its own abstract or virtual method gets the
		// subclass's version (base.X() is not virtual and keeps the base's).
		if (!(at is IInvocationOperation { IsVirtual: false }))
		{
			m = MostDerived(m);
		}
		if (plans.TryGetValue(m, out MethodPlan value))
		{
			if (value.Library)
			{
				throw new CompileError(m.Name + " is a function of the Blueprint function library " + Type.Name + ", for other mods to call: put code its functions share in a static class instead", at.Syntax.GetLocation());
			}
			return value;
		}
		bool flag = m.IsStatic || chain.Any((INamedTypeSymbol t) => SymbolEqualityComparer.Default.Equals(t, m.ContainingType));
		if (!flag && NeoRuneExtended.Compiler.Symbols.IsSource(m.ContainingType) && m.ContainingType.IsAbstract)
		{
			MethodPlan methodPlan = new MethodPlan
			{
				Method = m,
				Name = VariableName(m),
				External = true,
				ByName = true
			};
			foreach (IParameterSymbol parameter in m.Parameters)
			{
				UType? obj = Symbols.Map(parameter.Type) ?? throw new CompileError($"parameter type '{parameter.Type}' has no Unreal equivalent", parameter.Locations.FirstOrDefault());
				List<(ISymbol, string, UType, bool)> list = methodPlan.Params;
				ISymbol item = parameter;
				string name = parameter.Name;
				UType item2 = obj;
				RefKind refKind = parameter.RefKind;
				bool item3 = refKind - 1 <= RefKind.Ref;
				list.Add((item, name, item2, item3));
			}
			if (!m.ReturnsVoid)
			{
				methodPlan.ReturnName = "ReturnValue";
				methodPlan.ReturnType = Symbols.Map(m.ReturnType);
			}
			return methodPlan;
		}
		if (!flag && NeoRuneExtended.Compiler.Symbols.IsSource(m.ContainingType))
		{
			MethodPlan methodPlan2 = Mod.ClassFor(m.ContainingType).Callable(m, at);
			return new MethodPlan
			{
				Method = m,
				Name = methodPlan2.Name,
				External = true
			}.CopySignature(methodPlan2);
		}
		if (!flag)
		{
			throw new CompileError("'" + m.ToDisplayString() + "' cannot be called from a mod", at.Syntax.GetLocation());
		}
		return Plan(m);
	}

	/// <summary>The override of a virtual or abstract method in this class or the abstract classes it merges, most derived first.</summary>
	private IMethodSymbol MostDerived(IMethodSymbol m)
	{
		if (m.IsStatic || (!m.IsVirtual && !m.IsAbstract && !m.IsOverride))
		{
			return m;
		}
		foreach (INamedTypeSymbol item in chain)
		{
			foreach (IMethodSymbol item2 in item.GetMembers(m.Name).OfType<IMethodSymbol>())
			{
				if (item2.IsAbstract)
				{
					continue;
				}
				for (IMethodSymbol methodSymbol = item2; methodSymbol != null; methodSymbol = methodSymbol.OverriddenMethod)
				{
					if (SymbolEqualityComparer.Default.Equals(methodSymbol.OriginalDefinition, m))
					{
						return item2;
					}
				}
			}
		}
		return m;
	}

	/// <summary>
	/// A public static method of a Blueprint function library, as the Blueprint editor makes its functions: static, named as
	/// in C# (or [UName]), with the hidden __WorldContext object after the inputs and before the outputs, which is the order
	/// Blueprint calls pass arguments in. A method that needs the world context declares it, as a UObject parameter named
	/// __WorldContext in that place.
	/// </summary>
	private void PlanLibraryFunction(IMethodSymbol m)
	{
		string name = NeoRuneExtended.Compiler.Symbols.UnrealName(m);
		if (functionNames.Contains(name))
		{
			throw new CompileError(Type.Name + " has two functions named " + name + " (Blueprint functions can't be overloaded)", m.Locations.FirstOrDefault());
		}
		if (m.IsGenericMethod)
		{
			throw new CompileError("generic methods are not supported (" + m.Name + ")", m.Locations.FirstOrDefault());
		}
		functionNames.Add(name);
		MethodPlan methodPlan = new MethodPlan
		{
			Method = m,
			Name = name,
			Library = true
		};
		FunctionBuilder functionBuilder = (methodPlan.Builder = Blueprint.AddFunction(name, EFunctionFlags.FUNC_Static | EFunctionFlags.FUNC_Public | EFunctionFlags.FUNC_BlueprintCallable | EFunctionFlags.FUNC_BlueprintEvent));
		bool worldContext = m.Parameters.Any((IParameterSymbol p) => p.Name == "__WorldContext");
		foreach (IParameterSymbol parameter in m.Parameters)
		{
			UType uType = Symbols.Map(parameter.Type) ?? throw new CompileError($"parameter type '{parameter.Type}' has no Unreal equivalent", parameter.Locations.FirstOrDefault());
			bool isOut = parameter.RefKind == RefKind.Out || parameter.RefKind == RefKind.Ref;
			if (isOut && !worldContext)
			{
				functionBuilder.AddParameter("__WorldContext", new UType.Object("/Script/CoreUObject.Object"));
				worldContext = true;
			}
			functionBuilder.AddParameter(parameter.Name, uType, isOut);
			methodPlan.Params.Add((parameter, parameter.Name, uType, isOut));
		}
		if (!worldContext)
		{
			functionBuilder.AddParameter("__WorldContext", new UType.Object("/Script/CoreUObject.Object"));
		}
		if (!m.ReturnsVoid)
		{
			UType returnType = Symbols.Map(m.ReturnType) ?? throw new CompileError($"return type '{m.ReturnType}' has no Unreal equivalent", m.Locations.FirstOrDefault());
			functionBuilder.AddParameter("ReturnValue", returnType, isOut: false, isReturn: true);
			methodPlan.ReturnName = "ReturnValue";
			methodPlan.ReturnType = returnType;
		}
		plans[m] = methodPlan;
		pending.Enqueue(methodPlan);
	}

	private MethodPlan Plan(IMethodSymbol m)
	{
		if (plans.TryGetValue(m, out MethodPlan value))
		{
			return value;
		}
		if (m.IsGenericMethod)
		{
			throw new CompileError("generic methods are not supported (" + m.Name + ")", m.Locations.FirstOrDefault());
		}
		UFunctionInfo uFunctionInfo = (m.IsOverride ? FindBound(m) : null);
		string text;
		uint flags;
		if (uFunctionInfo != null)
		{
			text = uFunctionInfo.Name;
			flags = uFunctionInfo.Flags & 0x5C0E0800;
		}
		else
		{
			IMethodSymbol methodSymbol = InterfaceMember(m);
			if (methodSymbol != null)
			{
				text = methodSymbol.Name;
				if (functionNames.Contains(text))
				{
					throw new CompileError($"{Type.Name} has two functions named {text}: rename the one that doesn't implement {methodSymbol.ContainingType.Name}", m.Locations.FirstOrDefault());
				}
				flags = 201457664u;
			}
			else
			{
				string text2 = ((m.MethodKind == MethodKind.AnonymousFunction) ? ("Lambda_" + m.ContainingSymbol.Name) : (m.IsStatic ? (m.ContainingType.Name + "_" + m.Name) : VariableName(m)));
				text = text2;
				int num = 2;
				while (functionNames.Contains(text))
				{
					text = $"{text2}_{num}";
					num++;
				}
				flags = 67239937u;
			}
		}
		functionNames.Add(text);
		MethodPlan methodPlan = new MethodPlan
		{
			Method = m,
			Name = text
		};
		FunctionBuilder functionBuilder = (methodPlan.Builder = Blueprint.AddFunction(text, (EFunctionFlags)flags, uFunctionInfo?.Path));
		if (uFunctionInfo != null)
		{
			List<UParam> list = uFunctionInfo.Inputs.ToList();
			for (int i = 0; i < list.Count; i++)
			{
				UParam uParam = list[i];
				functionBuilder.AddParameter(uParam.Name, uParam.Type, uParam.IsOut);
				methodPlan.Params.Add((m.Parameters[i], uParam.Name, uParam.Type, uParam.IsOut));
			}
			UParam uParam2 = uFunctionInfo.Return;
			if ((object)uParam2 != null)
			{
				functionBuilder.AddParameter(uParam2.Name, uParam2.Type, isOut: false, isReturn: true);
				methodPlan.ReturnName = uParam2.Name;
				methodPlan.ReturnType = uParam2.Type;
			}
		}
		else
		{
			foreach (IParameterSymbol parameter in m.Parameters)
			{
				UType uType = Symbols.Map(parameter.Type) ?? throw new CompileError($"parameter type '{parameter.Type}' has no Unreal equivalent", parameter.Locations.FirstOrDefault());
				RefKind refKind = parameter.RefKind;
				bool flag = refKind - 1 <= RefKind.Ref;
				bool flag2 = flag;
				functionBuilder.AddParameter(parameter.Name, uType, flag2);
				methodPlan.Params.Add((parameter, parameter.Name, uType, flag2));
			}
			if (!m.ReturnsVoid)
			{
				UType uType2 = Symbols.Map(m.ReturnType) ?? throw new CompileError($"return type '{m.ReturnType}' has no Unreal equivalent", m.Locations.FirstOrDefault());
				functionBuilder.AddParameter("ReturnValue", uType2, isOut: false, isReturn: true);
				methodPlan.ReturnName = "ReturnValue";
				methodPlan.ReturnType = uType2;
			}
		}
		plans[m] = methodPlan;
		pending.Enqueue(methodPlan);
		return methodPlan;
	}

	private UFunctionInfo? FindBound(IMethodSymbol m)
	{
		for (IMethodSymbol overriddenMethod = m.OverriddenMethod; overriddenMethod != null; overriddenMethod = overriddenMethod.OverriddenMethod)
		{
			UFunctionInfo uFunctionInfo = Symbols.Function(overriddenMethod);
			if ((object)uFunctionInfo != null)
			{
				return uFunctionInfo;
			}
		}
		return null;
	}
}
