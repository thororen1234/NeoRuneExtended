using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace NeoRuneExtended.Analyzers;

[DiagnosticAnalyzer("C#", new string[] { })]
public sealed class ModAnalyzer : DiagnosticAnalyzer
{
	private const string Category = "NeoRune";

	public static readonly DiagnosticDescriptor Exceptions = Rule("NR1001", "Exceptions are not supported", "Blueprint bytecode has no exceptions: remove '{0}' and check values instead");

	public static readonly DiagnosticDescriptor Lambdas = Rule("NR1002", "Lambdas can't capture locals", "'{0}' belongs to the enclosing method: lambdas and local functions can't capture its locals or parameters (Blueprint functions have no closures); store it in a field");

	public static readonly DiagnosticDescriptor Async = Rule("NR1003", "async/await is not supported", "async/await is not supported: use NeoRune.Timer to run code later");

	public static readonly DiagnosticDescriptor Linq = Rule("NR1004", "LINQ is not supported", "LINQ is not supported: use a foreach loop");

	public static readonly DiagnosticDescriptor StaticState = Rule("NR1005", "Static fields are not supported", "Static field '{0}' is not supported (Blueprints have no static state): make it an instance field of the mod class, or const");

	public static readonly DiagnosticDescriptor ScriptOnly = Rule("NR1006", "AngelScript function returns a value", "'{0}' is implemented in AngelScript and {1}: mods can call AngelScript functions only when they return nothing; read the value from a property instead");

	public static readonly DiagnosticDescriptor Generics = Rule("NR1007", "Generic methods are not supported", "Generic method '{0}' is not supported in mod code");

	public static readonly DiagnosticDescriptor Statement = Rule("NR1008", "Statement is not supported", "'{0}' is not supported in Blueprint bytecode");

	public static readonly DiagnosticDescriptor TypeOf = Rule("NR1009", "typeof is not supported", "Use Unreal.ClassOf<T>() to get an Unreal class");

	public static readonly DiagnosticDescriptor NoUnrealType = Rule("NR1010", "Type has no Unreal equivalent", "Type '{0}' has no Unreal equivalent and cannot be used in a mod");

	public static readonly DiagnosticDescriptor LostCopy = new DiagnosticDescriptor("NR1011", "Change to a copy is lost", "{0} is a copy (lists, dictionaries, sets and structs are values), so this change doesn't reach {1} and is never used", "NeoRune", DiagnosticSeverity.Warning, true, null, null);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(new DiagnosticDescriptor[11]
	{
		Exceptions, Lambdas, Async, Linq, StaticState, ScriptOnly, Generics, Statement, TypeOf, NoUnrealType,
		LostCopy
	});

	private static DiagnosticDescriptor Rule(string id, string title, string message)
	{
		return new DiagnosticDescriptor(id, title, message, "NeoRune", DiagnosticSeverity.Error, true, null, null);
	}

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterSyntaxNodeAction<SyntaxKind>(delegate(SyntaxNodeAnalysisContext c)
		{
			// Hand-written [UFunction] bindings are declarations only (=> throw null!): nothing of theirs is compiled.
			if (c.ContainingSymbol == null || !c.ContainingSymbol.GetAttributes().Any((AttributeData a) => a.AttributeClass?.Name == "UFunctionAttribute"))
			{
				c.ReportDiagnostic(Diagnostic.Create(Exceptions, c.Node.GetFirstToken().GetLocation(), c.Node.GetFirstToken().Text));
			}
		}, SyntaxKind.TryStatement, SyntaxKind.ThrowStatement, SyntaxKind.ThrowExpression);
		context.RegisterOperationAction(delegate(OperationAnalysisContext c)
		{
			AnalyzeCaptures(c, ((IAnonymousFunctionOperation)c.Operation).Symbol);
		}, OperationKind.AnonymousFunction);
		context.RegisterOperationAction(delegate(OperationAnalysisContext c)
		{
			AnalyzeCaptures(c, ((ILocalFunctionOperation)c.Operation).Symbol);
		}, OperationKind.LocalFunction);
		context.RegisterSyntaxNodeAction<SyntaxKind>(delegate(SyntaxNodeAnalysisContext c)
		{
			c.ReportDiagnostic(Diagnostic.Create(Async, c.Node.GetLocation()));
		}, SyntaxKind.AwaitExpression);
		context.RegisterSyntaxNodeAction<SyntaxKind>(delegate(SyntaxNodeAnalysisContext c)
		{
			c.ReportDiagnostic(Diagnostic.Create(Linq, c.Node.GetLocation()));
		}, SyntaxKind.QueryExpression);
		context.RegisterSyntaxNodeAction<SyntaxKind>(delegate(SyntaxNodeAnalysisContext c)
		{
			// An attribute argument is read by the compiler, not run: [ModSetting.Widget("id", typeof(MyWidget))] is fine.
			if (c.Node.FirstAncestorOrSelf<AttributeSyntax>() == null)
			{
				c.ReportDiagnostic(Diagnostic.Create(TypeOf, c.Node.GetLocation()));
			}
		}, SyntaxKind.TypeOfExpression);
		context.RegisterSyntaxNodeAction<SyntaxKind>(delegate(SyntaxNodeAnalysisContext c)
		{
			c.ReportDiagnostic(Diagnostic.Create(Statement, c.Node.GetFirstToken().GetLocation(), c.Node.GetFirstToken().Text));
		}, SyntaxKind.GotoStatement, SyntaxKind.LockStatement, SyntaxKind.UsingStatement, SyntaxKind.UnsafeStatement, SyntaxKind.FixedStatement, SyntaxKind.YieldReturnStatement);
		context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
		context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
		context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
		context.RegisterOperationAction(AnalyzeLocal, OperationKind.VariableDeclarator);
		context.RegisterOperationAction(AnalyzeCopies, OperationKind.MethodBody, OperationKind.LocalFunction);
	}

	private static bool InModCode(ISymbol s)
	{
		return s.Locations.Any<Location>((Location l) => l.IsInSource);
	}

	private static void AnalyzeField(SymbolAnalysisContext c)
	{
		IFieldSymbol fieldSymbol = (IFieldSymbol)c.Symbol;
		if (!fieldSymbol.IsImplicitlyDeclared && fieldSymbol.ContainingType.TypeKind != TypeKind.Enum)
		{
			if (fieldSymbol.IsStatic && !fieldSymbol.IsConst)
			{
				c.ReportDiagnostic(Diagnostic.Create(StaticState, fieldSymbol.Locations[0], fieldSymbol.Name));
			}
			else if (!fieldSymbol.IsConst && !IsUnrealType(fieldSymbol.Type))
			{
				c.ReportDiagnostic(Diagnostic.Create(NoUnrealType, fieldSymbol.Locations[0], fieldSymbol.Type.ToDisplayString()));
			}
		}
	}

	private static void AnalyzeMethod(SymbolAnalysisContext c)
	{
		IMethodSymbol methodSymbol = (IMethodSymbol)c.Symbol;
		if (methodSymbol.MethodKind != MethodKind.Ordinary || methodSymbol.IsImplicitlyDeclared)
		{
			return;
		}
		if (methodSymbol.IsAsync)
		{
			c.ReportDiagnostic(Diagnostic.Create(Async, methodSymbol.Locations[0]));
		}
		if (methodSymbol.IsGenericMethod)
		{
			c.ReportDiagnostic(Diagnostic.Create(Generics, methodSymbol.Locations[0], methodSymbol.Name));
		}
		foreach (IParameterSymbol parameter in methodSymbol.Parameters)
		{
			if (!IsUnrealType(parameter.Type))
			{
				c.ReportDiagnostic(Diagnostic.Create(NoUnrealType, parameter.Locations[0], parameter.Type.ToDisplayString()));
			}
		}
		if (!methodSymbol.ReturnsVoid && !IsUnrealType(methodSymbol.ReturnType))
		{
			c.ReportDiagnostic(Diagnostic.Create(NoUnrealType, methodSymbol.Locations[0], methodSymbol.ReturnType.ToDisplayString()));
		}
	}

	private static void AnalyzeInvocation(OperationAnalysisContext c)
	{
		IMethodSymbol targetMethod = ((IInvocationOperation)c.Operation).TargetMethod;
		// AngelScript functions are called through ProcessEvent, which gives nothing back: no return value, no out parameters.
		if (targetMethod.OriginalDefinition.GetAttributes().Any<AttributeData>((AttributeData a) => a.AttributeClass?.Name == "ScriptOnlyAttribute"))
		{
			string problem = !targetMethod.ReturnsVoid ? "returns a value" : (targetMethod.Parameters.Any((IParameterSymbol p) => p.RefKind == RefKind.Out || p.RefKind == RefKind.Ref) ? "has out parameters" : null);
			if (problem != null)
			{
				c.ReportDiagnostic(Diagnostic.Create(ScriptOnly, c.Operation.Syntax.GetLocation(), targetMethod.Name, problem));
			}
		}
		if (targetMethod.ContainingType?.ToDisplayString() == "System.Linq.Enumerable")
		{
			c.ReportDiagnostic(Diagnostic.Create(Linq, c.Operation.Syntax.GetLocation()));
		}
	}

	private static void AnalyzeCaptures(OperationAnalysisContext c, IMethodSymbol fn)
	{
		foreach (IOperation item in c.Operation.Descendants())
		{
			ISymbol symbol = ((item is ILocalReferenceOperation localReferenceOperation) ? ((ISymbol)localReferenceOperation.Local) : ((ISymbol)((!(item is IParameterReferenceOperation parameterReferenceOperation)) ? null : parameterReferenceOperation.Parameter)));
			ISymbol symbol2 = symbol;
			if (symbol2 != null)
			{
				ISymbol containingSymbol = symbol2.ContainingSymbol;
				while (containingSymbol != null && !SymbolEqualityComparer.Default.Equals(containingSymbol, fn))
				{
					containingSymbol = containingSymbol.ContainingSymbol;
				}
				if (containingSymbol == null)
				{
					c.ReportDiagnostic(Diagnostic.Create(Lambdas, item.Syntax.GetLocation(), symbol2.Name));
				}
			}
		}
	}

	private static void AnalyzeCopies(OperationAnalysisContext c)
	{
		Dictionary<ILocalSymbol, IOperation> dictionary = new Dictionary<ILocalSymbol, IOperation>(SymbolEqualityComparer.Default);
		Dictionary<ILocalSymbol, IOperation> dictionary2 = new Dictionary<ILocalSymbol, IOperation>(SymbolEqualityComparer.Default);
		HashSet<IOperation> changeReceivers = new HashSet<IOperation>();
		IOperation value = default(IOperation);
		foreach (IOperation item in c.Operation.Descendants())
		{
			IVariableDeclaratorOperation variableDeclaratorOperation = item as IVariableDeclaratorOperation;
			int num;
			if (variableDeclaratorOperation != null)
			{
				IVariableInitializerOperation initializer = variableDeclaratorOperation.Initializer;
				if (initializer != null)
				{
					value = initializer.Value;
					if (value != null)
					{
						num = (IsValueType(variableDeclaratorOperation.Symbol.Type) ? 1 : 0);
						goto IL_0083;
					}
				}
			}
			num = 0;
			goto IL_0083;
			IL_0083:
			bool flag = (byte)num != 0;
			if (flag)
			{
				IOperation operation = Unwrap(value);
				bool flag2 = ((operation is IFieldReferenceOperation || operation is IPropertyReferenceOperation || operation is IInvocationOperation) ? true : false);
				flag = flag2;
			}
			if (flag)
			{
				dictionary[variableDeclaratorOperation.Symbol] = Unwrap(value);
			}
			IOperation operation2 = ChangedTarget(item);
			if (operation2 == null)
			{
				continue;
			}
			if (operation2 is ILocalReferenceOperation localReferenceOperation)
			{
				changeReceivers.Add(localReferenceOperation);
				if (!dictionary2.ContainsKey(localReferenceOperation.Local))
				{
					dictionary2[localReferenceOperation.Local] = item;
				}
			}
			else if (operation2 is IInvocationOperation invocationOperation && IsValueType(invocationOperation.Type))
			{
				c.ReportDiagnostic(Diagnostic.Create(LostCopy, item.Syntax.GetLocation(), "The result of " + invocationOperation.TargetMethod.Name + "()", "the object it came from"));
			}
		}
		HashSet<ILocalSymbol> hashSet = new HashSet<ILocalSymbol>(from r in c.Operation.Descendants().OfType<ILocalReferenceOperation>()
			where !changeReceivers.Contains(r)
			select r.Local, SymbolEqualityComparer.Default);
		foreach (KeyValuePair<ILocalSymbol, IOperation> item2 in dictionary2)
		{
			if (dictionary.TryGetValue(item2.Key, out var value2) && !hashSet.Contains(item2.Key))
			{
				c.ReportDiagnostic(Diagnostic.Create(LostCopy, item2.Value.Syntax.GetLocation(), "'" + item2.Key.Name + "'", Describe(value2)));
			}
		}
	}

	private static string Describe(IOperation source)
	{
		if (!(source is IInvocationOperation))
		{
			return source.Syntax.ToString();
		}
		return "the object it came from";
	}

	private static IOperation Unwrap(IOperation op)
	{
		while (op is IConversionOperation conversionOperation)
		{
			op = conversionOperation.Operand;
		}
		return op;
	}

	private static bool IsValueType(ITypeSymbol t)
	{
		bool flag = t is INamedTypeSymbol namedTypeSymbol && t.TypeKind == TypeKind.Struct && t.SpecialType == SpecialType.None && !namedTypeSymbol.ContainingNamespace.ToDisplayString().StartsWith("System") && namedTypeSymbol.ContainingNamespace.ToDisplayString() != "NeoRune";
		INamedTypeSymbol namedTypeSymbol2;
		bool flag2;
		if (!flag)
		{
			namedTypeSymbol2 = t as INamedTypeSymbol;
			if (namedTypeSymbol2 == null)
			{
				goto IL_008f;
			}
			switch (t.MetadataName)
			{
			case "List`1":
			case "Dictionary`2":
			case "HashSet`1":
				break;
			default:
				goto IL_008f;
			}
			flag2 = true;
			goto IL_0092;
		}
		goto IL_00b0;
		IL_00b0:
		return flag;
		IL_008f:
		flag2 = false;
		goto IL_0092;
		IL_0092:
		flag = flag2 && namedTypeSymbol2.ContainingNamespace.ToDisplayString() == "System.Collections.Generic";
		goto IL_00b0;
	}

	private static IOperation? ChangedTarget(IOperation op)
	{
		if (op is IInvocationOperation invocationOperation)
		{
			IMethodSymbol targetMethod = invocationOperation.TargetMethod;
			if (targetMethod != null)
			{
				switch (targetMethod.Name)
				{
				case "Add":
				case "Remove":
				case "RemoveAt":
				case "Insert":
				case "Clear":
				{
					IOperation instance = invocationOperation.Instance;
					if (instance != null && IsValueType(instance.Type))
					{
						return Unwrap(instance);
					}
					break;
				}
				}
			}
		}
		IOperation instance3;
		if (op is IAssignmentOperation assignmentOperation)
		{
			IOperation target = assignmentOperation.Target;
			if (target is IPropertyReferenceOperation propertyReferenceOperation)
			{
				IPropertySymbol property = propertyReferenceOperation.Property;
				if (property != null && property.IsIndexer)
				{
					IOperation instance2 = propertyReferenceOperation.Instance;
					if (instance2 != null)
					{
						if (IsValueType(instance2.Type))
						{
							return Unwrap(instance2);
						}
						if (target is IFieldReferenceOperation fieldReferenceOperation)
						{
							instance3 = fieldReferenceOperation.Instance;
							goto IL_0120;
						}
					}
					goto IL_0136;
				}
			}
			if (target is IFieldReferenceOperation fieldReferenceOperation2)
			{
				instance3 = fieldReferenceOperation2.Instance;
				if (instance3 != null)
				{
					goto IL_0120;
				}
			}
		}
		goto IL_0136;
		IL_0136:
		return null;
		IL_0120:
		if (IsValueType(instance3.Type))
		{
			return Unwrap(instance3);
		}
		goto IL_0136;
	}

	private static void AnalyzeLocal(OperationAnalysisContext c)
	{
		IVariableDeclaratorOperation variableDeclaratorOperation = (IVariableDeclaratorOperation)c.Operation;
		if (variableDeclaratorOperation.Parent is IForEachLoopOperation)
		{
			ITypeSymbol type = variableDeclaratorOperation.Symbol.Type;
			if (type is INamedTypeSymbol namedTypeSymbol && type.MetadataName == "KeyValuePair`2" && namedTypeSymbol.ContainingNamespace?.ToDisplayString() == "System.Collections.Generic" && namedTypeSymbol.TypeArguments.All(IsUnrealType))
			{
				return;
			}
		}
		if (!IsUnrealType(variableDeclaratorOperation.Symbol.Type))
		{
			c.ReportDiagnostic(Diagnostic.Create(NoUnrealType, variableDeclaratorOperation.Syntax.GetLocation(), variableDeclaratorOperation.Symbol.Type.ToDisplayString()));
		}
	}

	private static bool IsUnrealType(ITypeSymbol t)
	{
		if (t is IErrorTypeSymbol)
		{
			return true;
		}
		switch (t.SpecialType)
		{
		case SpecialType.System_Boolean:
		case SpecialType.System_SByte:
		case SpecialType.System_Byte:
		case SpecialType.System_Int16:
		case SpecialType.System_UInt16:
		case SpecialType.System_Int32:
		case SpecialType.System_UInt32:
		case SpecialType.System_Int64:
		case SpecialType.System_UInt64:
		case SpecialType.System_Single:
		case SpecialType.System_Double:
		case SpecialType.System_String:
			return true;
		default:
		{
			if (!(t is INamedTypeSymbol namedTypeSymbol))
			{
				return false;
			}
			if (namedTypeSymbol.TypeKind == TypeKind.Enum && namedTypeSymbol.EnumUnderlyingType != null)
			{
				return true;
			}
			if (namedTypeSymbol.TypeKind == TypeKind.Struct && namedTypeSymbol.Locations.Any<Location>((Location l) => l.IsInSource))
			{
				return true;
			}
			string text = namedTypeSymbol.ContainingNamespace?.ToDisplayString();
			bool flag = text == "NeoRune";
			if (flag)
			{
				bool flag2;
				switch (namedTypeSymbol.MetadataName)
				{
				case "FName":
				case "FText":
				case "TSubclassOf`1":
				case "TSoftObjectPtr`1":
				case "TSoftClassPtr`1":
				case "TWeakObjectPtr`1":
					flag2 = true;
					break;
				default:
					flag2 = false;
					break;
				}
				flag = flag2;
			}
			if (flag)
			{
				return true;
			}
			flag = text == "System.Collections.Generic";
			if (flag)
			{
				bool flag2;
				switch (namedTypeSymbol.MetadataName)
				{
				case "List`1":
				case "HashSet`1":
				case "Dictionary`2":
					flag2 = true;
					break;
				default:
					flag2 = false;
					break;
				}
				flag = flag2;
			}
			if (flag)
			{
				return namedTypeSymbol.TypeArguments.All(IsUnrealType);
			}
			for (INamedTypeSymbol namedTypeSymbol2 = namedTypeSymbol; namedTypeSymbol2 != null; namedTypeSymbol2 = namedTypeSymbol2.BaseType)
			{
				if (namedTypeSymbol2.GetAttributes().Any<AttributeData>(delegate(AttributeData a)
				{
					switch (a.AttributeClass?.Name)
					{
					case "UClassAttribute":
					case "UStructAttribute":
					case "UEnumAttribute":
					case "UDelegateAttribute":
						return true;
					default:
						return false;
					}
				}))
				{
					return true;
				}
			}
			return false;
		}
		}
	}
}
