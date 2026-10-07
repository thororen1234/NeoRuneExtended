using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using NeoRuneExtended.Assets;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.UnrealTypes;

namespace NeoRuneExtended.Compiler;

internal sealed class FunctionCompiler
{
	private readonly ClassCompiler cls;

	private readonly MethodPlan plan;

	private readonly FunctionBuilder fn;

	private readonly ScriptBuilder s;

	private readonly Symbols sym;

	private readonly PackageBuilder pkg;

	private readonly Dictionary<ISymbol, LocalVar> locals = new Dictionary<ISymbol, LocalVar>(SymbolEqualityComparer.Default);

	private readonly Stack<(Label Break, Label Continue)> loops = new Stack<(Label, Label)>();

	private readonly Label returnLabel;

	private int temps;

	private bool failed;

	private readonly Dictionary<ISymbol, (LocalVar Key, LocalVar Value)> pairs = new Dictionary<ISymbol, (LocalVar, LocalVar)>(SymbolEqualityComparer.Default);

	private static readonly ImmutableArray<IOperation> ImmutableEmpty = ImmutableArray<IOperation>.Empty;

	private LocalVar? pendingResult;

	private readonly Stack<Var> conditionalInstances = new Stack<Var>();

	public FunctionCompiler(ClassCompiler cls, MethodPlan plan)
	{
		this.cls = cls;
		this.plan = plan;
		fn = plan.Builder;
		s = fn.Script;
		sym = cls.Symbols;
		pkg = cls.Blueprint.Package;
		returnLabel = s.NewLabel("return");
		foreach (var (key, name, type, outParam) in plan.Params)
		{
			locals[key] = new LocalVar(name, type, fn.Index, outParam);
		}
	}

	private static CompileError Error(IOperation? op, string message)
	{
		return new CompileError(message, op?.Syntax.GetLocation());
	}

	public void CompileBody(IOperation? body, IEnumerable<IOperation>? prelude = null, IMethodSymbol? startup = null)
	{
		if (startup != null)
		{
			MethodPlan methodPlan = cls.Callable(startup, null);
			s.Emit(new EX_LocalFinalFunction
			{
				StackNode = methodPlan.Index,
				Parameters = new KismetExpression[1]
				{
					new EX_Self()
				}
			});
		}
		if (prelude != null)
		{
			foreach (IOperation item in prelude)
			{
				Statement(item);
			}
		}
		if (body != null)
		{
			Statement(body);
		}
		s.Mark(returnLabel);
		if (failed)
		{
			throw CompileError.Reported();
		}
	}

	private LocalVar DeclareLocal(ILocalSymbol local, IOperation at)
	{
		if (locals.TryGetValue(local, out LocalVar value))
		{
			return value;
		}
		UType type = sym.Map(local.Type) ?? throw Error(at, $"type '{local.Type}' has no Unreal equivalent");
		string name = local.Name;
		int num = 2;
		while (fn.HasLocal(name))
		{
			name = $"{local.Name}_{num}";
			num++;
		}
		fn.AddLocal(name, type);
		return locals[local] = new LocalVar(name, type, fn.Index);
	}

	private LocalVar Temp(UType type)
	{
		string name = $"__tmp{temps++}";
		fn.AddLocal(name, type);
		return new LocalVar(name, type, fn.Index);
	}

	private LocalVar ZeroOf(UType.Struct t)
	{
		string name = "__zero_" + t.StructPath.Split('.')[^1];
		if (!fn.HasLocal(name))
		{
			fn.AddLocal(name, t);
		}
		return new LocalVar(name, t, fn.Index);
	}

	private void Statement(IOperation op)
	{
		if (!(op is IBlockOperation blockOperation))
		{
			if (!(op is IVariableDeclarationGroupOperation variableDeclarationGroupOperation))
			{
				if (!(op is ILocalFunctionOperation localFunctionOperation))
				{
					if (!(op is IExpressionStatementOperation expressionStatementOperation))
					{
						if (!(op is IConditionalOperation conditionalOperation))
						{
							if (!(op is IWhileLoopOperation whileLoopOperation))
							{
								if (!(op is IForLoopOperation forLoopOperation))
								{
									if (!(op is IForEachLoopOperation fe))
									{
										if (!(op is IBranchOperation branchOperation))
										{
											if (!(op is IReturnOperation returnOperation))
											{
												if (!(op is ISwitchOperation sw))
												{
													if (op is IEmptyOperation)
													{
														return;
													}
													if (!(op is IFieldInitializerOperation fieldInitializerOperation))
													{
														if (!(op is IPropertyInitializerOperation propertyInitializerOperation))
														{
															if (!(op is ILabeledOperation { Operation: not null } labeledOperation))
															{
																if (!(op is IThrowOperation))
																{
																	if (!(op is ITryOperation))
																	{
																		if (op is ILockOperation || op is IUsingOperation)
																		{
																			throw Error(op, $"'{op.Syntax}' is not supported");
																		}
																		throw Error(op, $"unsupported statement ({op.Kind})");
																	}
																	throw Error(op, "try/catch is not supported in Blueprint bytecode");
																}
																throw Error(op, "exceptions are not supported in Blueprint bytecode");
															}
															Statement(labeledOperation.Operation);
														}
														else
														{
															foreach (IPropertySymbol initializedProperty in propertyInitializerOperation.InitializedProperties)
															{
																Assign(cls.Field(initializedProperty) ?? throw Error(propertyInitializerOperation, "unsupported property initializer"), propertyInitializerOperation.Value);
															}
														}
													}
													else
													{
														foreach (IFieldSymbol initializedField in fieldInitializerOperation.InitializedFields)
														{
															Assign((Var)Member(initializedField, null, fieldInitializerOperation), fieldInitializerOperation.Value);
														}
													}
												}
												else
												{
													Switch(sw);
												}
											}
											else
											{
												if (returnOperation.ReturnedValue != null)
												{
													string name = plan.ReturnName ?? throw Error(op, "return value in a void function");
													Assign(new LocalVar(name, plan.ReturnType, fn.Index, OutParam: true), returnOperation.ReturnedValue);
												}
												s.Jump(returnLabel);
											}
											return;
										}
										if (loops.Count == 0)
										{
											throw Error(op, "break/continue outside a loop");
										}
										if (branchOperation.BranchKind == BranchKind.Break)
										{
											s.Jump(loops.Peek().Break);
											return;
										}
										if (branchOperation.BranchKind != BranchKind.Continue)
										{
											throw Error(op, "goto is not supported");
										}
										s.Jump(loops.Peek().Continue);
									}
									else
									{
										ForEach(fe);
									}
								}
								else
								{
									foreach (IOperation item in forLoopOperation.Before)
									{
										Statement(item);
									}
									Label label = s.NewLabel("for");
									Label label2 = s.NewLabel("continue");
									Label label3 = s.NewLabel("endfor");
									loops.Push((label3, label2));
									s.Mark(label);
									if (forLoopOperation.Condition != null)
									{
										Branch(forLoopOperation.Condition, label3);
									}
									Statement(forLoopOperation.Body);
									s.Mark(label2);
									foreach (IOperation item2 in forLoopOperation.AtLoopBottom)
									{
										Statement(item2);
									}
									s.Jump(label);
									s.Mark(label3);
									loops.Pop();
								}
								return;
							}
							Label label4 = s.NewLabel("loop");
							Label label5 = s.NewLabel("continue");
							Label label6 = s.NewLabel("endloop");
							loops.Push((label6, label5));
							if (whileLoopOperation.ConditionIsTop)
							{
								s.Mark(label4);
								s.Mark(label5);
								if (whileLoopOperation.Condition != null)
								{
									Branch(whileLoopOperation.Condition, label6, whileLoopOperation.ConditionIsUntil);
								}
								Statement(whileLoopOperation.Body);
								s.Jump(label4);
							}
							else
							{
								s.Mark(label4);
								Statement(whileLoopOperation.Body);
								s.Mark(label5);
								if (whileLoopOperation.Condition != null)
								{
									KismetExpression condition = Bool(whileLoopOperation.Condition, whileLoopOperation.ConditionIsUntil);
									s.JumpIfNot(condition, label6);
								}
								s.Jump(label4);
							}
							s.Mark(label6);
							loops.Pop();
						}
						else
						{
							Label label7 = s.NewLabel("else");
							Label label8 = s.NewLabel("endif");
							Branch(conditionalOperation.Condition, label7);
							Statement(conditionalOperation.WhenTrue);
							if (conditionalOperation.WhenFalse != null)
							{
								s.Jump(label8);
								s.Mark(label7);
								Statement(conditionalOperation.WhenFalse);
								s.Mark(label8);
							}
							else
							{
								s.Mark(label7);
							}
						}
					}
					else
					{
						Effect(expressionStatementOperation.Operation);
					}
				}
				else
				{
					CheckNoCaptures(localFunctionOperation.Symbol, localFunctionOperation.Body ?? localFunctionOperation.IgnoredBody);
					cls.Callable(localFunctionOperation.Symbol, localFunctionOperation);
				}
				return;
			}
			foreach (IVariableDeclarationOperation declaration in variableDeclarationGroupOperation.Declarations)
			{
				foreach (IVariableDeclaratorOperation declarator in declaration.Declarators)
				{
					LocalVar target = DeclareLocal(declarator.Symbol, declarator);
					IOperation operation = declarator.Initializer?.Value ?? declaration.Initializer?.Value;
					if (operation != null)
					{
						Assign(target, operation);
					}
				}
			}
			return;
		}
		foreach (IOperation operation2 in blockOperation.Operations)
		{
			try
			{
				Statement(operation2);
			}
			catch (CompileError e)
			{
				cls.Mod.Report(e);
				failed = true;
			}
		}
	}

	private void Branch(IOperation cond, Label target, bool invert = false)
	{
		s.JumpIfNot(Bool(cond, invert), target);
	}

	private KismetExpression Bool(IOperation cond, bool invert = false)
	{
		KismetExpression kismetExpression = Read(Lower(cond));
		if (!invert)
		{
			return kismetExpression;
		}
		return CallMath(LibFn("UKismetMathLibrary", "Not_PreBool"), kismetExpression);
	}

	private void Switch(ISwitchOperation sw)
	{
		Var left = AsVar(Lower(sw.Value));
		Label label = s.NewLabel("endswitch");
		loops.Push((label, (loops.Count > 0) ? loops.Peek().Continue : label));
		List<Label> list = sw.Cases.Select((ISwitchCaseOperation _) => s.NewLabel("case")).ToList();
		Label label2 = null;
		for (int num = 0; num < sw.Cases.Length; num++)
		{
			foreach (ICaseClauseOperation clause in sw.Cases[num].Clauses)
			{
				if (clause is IDefaultCaseClauseOperation)
				{
					label2 = list[num];
					continue;
				}
				if (!(clause is ISingleValueCaseClauseOperation singleValueCaseClauseOperation))
				{
					throw Error(clause, "only constant case labels are supported");
				}
				Label label3 = s.NewLabel("next");
				s.JumpIfNot(Read(Compare(BinaryOperatorKind.Equals, left, Lower(singleValueCaseClauseOperation.Value), clause)), label3);
				s.Jump(list[num]);
				s.Mark(label3);
			}
		}
		s.Jump(label2 ?? label);
		for (int num2 = 0; num2 < sw.Cases.Length; num2++)
		{
			s.Mark(list[num2]);
			foreach (IOperation item in sw.Cases[num2].Body)
			{
				Statement(item);
			}
		}
		s.Mark(label);
		loops.Pop();
	}

	private void ForEach(IForEachLoopOperation fe)
	{
		Value value = Lower((fe.Collection is IConversionOperation conversionOperation) ? conversionOperation.Operand : fe.Collection);
		LocalVar localVar = null;
		UType type = value.Type;
		Var list;
		if (!(type is UType.Array))
		{
			if (!(type is UType.Set set))
			{
				if (!(type is UType.Map map))
				{
					throw Error(fe, "foreach is supported over List<T>, HashSet<T> and Dictionary<K,V>");
				}
				Var container = AsVar(value);
				list = CopyToArray("Map_Keys", container, map.Key);
				localVar = CopyToArray("Map_Values", container, map.Value);
			}
			else
			{
				list = CopyToArray("Set_ToArray", AsVar(value), set.Element);
			}
		}
		else
		{
			list = AsVar(value);
		}
		LocalVar localVar2 = Temp(UType.Int);
		LocalVar localVar3 = Temp(UType.Int);
		s.Emit(s.Let(UType.Int, localVar2.Expr(s), ScriptBuilder.Int(0), localVar2.Pointer(s)));
		Effect(ArrayCall("Array_Length", list, localVar3, Array.Empty<KismetExpression>()), localVar3);
		LocalVar localVar4 = null;
		LocalVar localVar5;
		if (localVar != null)
		{
			(localVar5, localVar4) = DeclarePair(fe.LoopControlVariable, (UType.Map)value.Type, fe);
		}
		else
		{
			if (!(fe.LoopControlVariable is IVariableDeclaratorOperation variableDeclaratorOperation))
			{
				throw Error(fe, "unsupported foreach variable (deconstruction works over Dictionary only)");
			}
			localVar5 = DeclareLocal(variableDeclaratorOperation.Symbol, variableDeclaratorOperation);
		}
		Label label = s.NewLabel("foreach");
		Label label2 = s.NewLabel("continue");
		Label label3 = s.NewLabel("endforeach");
		loops.Push((label3, label2));
		s.Mark(label);
		s.JumpIfNot(CallMath(LibFn("UKismetMathLibrary", "Less_IntInt"), localVar2.Expr(s), localVar3.Expr(s)), label3);
		Effect(ArrayCall("Array_Get", list, null, new KismetExpression[2]
		{
			localVar2.Expr(s),
			localVar5.Expr(s)
		}), null);
		if (localVar != null)
		{
			Effect(ArrayCall("Array_Get", localVar, null, new KismetExpression[2]
			{
				localVar2.Expr(s),
				localVar4.Expr(s)
			}), null);
		}
		Statement(fe.Body);
		s.Mark(label2);
		s.Emit(s.Let(UType.Int, localVar2.Expr(s), CallMath(LibFn("UKismetMathLibrary", "Add_IntInt"), localVar2.Expr(s), ScriptBuilder.Int(1)), localVar2.Pointer(s)));
		s.Jump(label);
		s.Mark(label3);
		loops.Pop();
	}

	private LocalVar CopyToArray(string fnName, Var container, UType element)
	{
		LocalVar localVar = Temp(new UType.Array(element));
		s.Emit(fnName.StartsWith("Set_") ? SetCall(fnName, container, null, new KismetExpression[1] { localVar.Expr(s) }) : MapCall(fnName, container, null, new KismetExpression[1] { localVar.Expr(s) }));
		return localVar;
	}

	private (LocalVar Key, LocalVar Value) DeclarePair(IOperation control, UType.Map map, IOperation at)
	{
		if (control is IVariableDeclaratorOperation variableDeclaratorOperation)
		{
			LocalVar item = NamedTemp(variableDeclaratorOperation.Symbol.Name + "_Key", map.Key);
			LocalVar item2 = NamedTemp(variableDeclaratorOperation.Symbol.Name + "_Value", map.Value);
			pairs[variableDeclaratorOperation.Symbol] = (item, item2);
			return (Key: item, Value: item2);
		}
		ITupleOperation tupleOperation = ((control is IDeclarationExpressionOperation { Expression: ITupleOperation expression }) ? expression : (control as ITupleOperation));
		if (tupleOperation == null || tupleOperation.Elements.Length != 2)
		{
			throw Error(at, "use 'foreach (var pair in map)' or 'foreach (var (key, value) in map)'");
		}
		return (Key: Element(tupleOperation.Elements[0], map.Key), Value: Element(tupleOperation.Elements[1], map.Value));
		LocalVar Element(IOperation e, UType type)
		{
			if (e is IDeclarationExpressionOperation { Expression: ILocalReferenceOperation expression2 })
			{
				return DeclareLocal(expression2.Local, e);
			}
			if (e is ILocalReferenceOperation localReferenceOperation)
			{
				LocalVar value;
				return locals.TryGetValue(localReferenceOperation.Local, out value) ? value : DeclareLocal(localReferenceOperation.Local, e);
			}
			if (!(e is IDiscardOperation))
			{
				throw Error(e, "each part of the deconstruction must be a new variable or _");
			}
			return Temp(type);
		}
	}

	private LocalVar NamedTemp(string baseName, UType type)
	{
		string name = baseName;
		int num = 2;
		while (fn.HasLocal(name))
		{
			name = $"{baseName}_{num}";
			num++;
		}
		fn.AddLocal(name, type);
		return new LocalVar(name, type, fn.Index);
	}

	private void Effect(IOperation op)
	{
		ICompoundAssignmentOperation compoundAssignmentOperation;
		if (!(op is ISimpleAssignmentOperation simpleAssignmentOperation))
		{
			compoundAssignmentOperation = op as ICompoundAssignmentOperation;
			IIncrementOrDecrementOperation incrementOrDecrementOperation;
			int num;
			if (compoundAssignmentOperation != null)
			{
				ICompoundAssignmentOperation compoundAssignmentOperation2 = compoundAssignmentOperation;
				if (IsAccessorProperty(compoundAssignmentOperation2.Target, out IPropertyReferenceOperation pr))
				{
					Value accessor = GetAccessor(pr);
					SetAccessor(pr, Binary(compoundAssignmentOperation2.OperatorKind, accessor, Lower(compoundAssignmentOperation2.Value), accessor.Type, compoundAssignmentOperation2));
					return;
				}
				incrementOrDecrementOperation = op as IIncrementOrDecrementOperation;
				if (incrementOrDecrementOperation == null)
				{
					goto IL_0137;
				}
				num = 1;
			}
			else
			{
				incrementOrDecrementOperation = op as IIncrementOrDecrementOperation;
				if (incrementOrDecrementOperation == null)
				{
					if (!(op is IInvocationOperation invocationOperation))
					{
						if (!(op is IEventAssignmentOperation eventAssignmentOperation))
						{
							if (!(op is IConditionalAccessOperation conditionalAccessOperation))
							{
								if (!(op is IDiscardOperation))
								{
									RValue rValue = Lower(op) as RValue;
									bool flag = (object)rValue != null;
									if (flag)
									{
										KismetExpression expr = rValue.Expr;
										bool flag2 = ((expr is EX_CallMath || expr is EX_FinalFunction || expr is EX_VirtualFunction || expr is EX_Context) ? true : false);
										flag = flag2;
									}
									if (flag)
									{
										s.Emit(rValue.Expr);
									}
								}
							}
							else
							{
								IConditionalAccessOperation ca = conditionalAccessOperation;
								ConditionalAccess(ca, wantValue: false);
							}
						}
						else
						{
							IEventAssignmentOperation ev = eventAssignmentOperation;
							EventAssign(ev);
						}
					}
					else
					{
						IInvocationOperation inv = invocationOperation;
						Invoke(inv, wantResult: false);
					}
					return;
				}
				num = 2;
			}
			IIncrementOrDecrementOperation incrementOrDecrementOperation2 = incrementOrDecrementOperation;
			if (!IsAccessorProperty(incrementOrDecrementOperation2.Target, out IPropertyReferenceOperation pr2))
			{
				if (num == 1)
				{
					goto IL_0137;
				}
				if (num == 2)
				{
					IIncrementOrDecrementOperation incrementOrDecrementOperation3 = incrementOrDecrementOperation;
					Var var = LowerTarget(incrementOrDecrementOperation3.Target);
					Store(var, Read(Step(var, incrementOrDecrementOperation3.Kind == OperationKind.Increment, incrementOrDecrementOperation3)));
					return;
				}
			}
			Var v = AsVar(GetAccessor(pr2));
			SetAccessor(pr2, Step(v, incrementOrDecrementOperation2.Kind == OperationKind.Increment, incrementOrDecrementOperation2));
		}
		else
		{
			ISimpleAssignmentOperation simpleAssignmentOperation2 = simpleAssignmentOperation;
			AssignTo(simpleAssignmentOperation2.Target, simpleAssignmentOperation2.Value);
		}
		return;
		IL_0137:
		ICompoundAssignmentOperation compoundAssignmentOperation3 = compoundAssignmentOperation;
		Var var2 = LowerTarget(compoundAssignmentOperation3.Target);
		Store(var2, Read(Binary(compoundAssignmentOperation3.OperatorKind, var2, Lower(compoundAssignmentOperation3.Value), var2.Type, compoundAssignmentOperation3)));
	}

	private void AssignTo(IOperation targetOp, IOperation valueOp)
	{
		if (IsAccessorProperty(targetOp, out IPropertyReferenceOperation pr))
		{
			SetAccessor(pr, Lower(valueOp));
			return;
		}
		if (targetOp is IPropertyReferenceOperation propertyReferenceOperation)
		{
			IPropertySymbol property = propertyReferenceOperation.Property;
			if (property != null && property.IsIndexer)
			{
				Var var = AsVar(Lower(propertyReferenceOperation.Instance));
				UType uType = ((var.Type is UType.Map map) ? map.Key : UType.Int);
				KismetExpression kismetExpression = RefSafe(Coerce(Lower(propertyReferenceOperation.Arguments[0].Value), uType, propertyReferenceOperation), uType);
				if (var.Type is UType.Array array)
				{
					Effect(ArrayCall("Array_Set", var, null, new KismetExpression[3]
					{
						kismetExpression,
						RefSafe(Coerce(Lower(valueOp), array.Inner, valueOp), array.Inner),
						ScriptBuilder.Bool(v: false)
					}), null);
					return;
				}
				if (var.Type is UType.Map map2)
				{
					Effect(MapCall("Map_Add", var, null, new KismetExpression[2]
					{
						kismetExpression,
						RefSafe(Coerce(Lower(valueOp), map2.Value, valueOp), map2.Value)
					}), null);
					return;
				}
				throw Error(targetOp, "unsupported indexer");
			}
		}
		Assign(LowerTarget(targetOp), valueOp);
	}

	private void Assign(Var target, IOperation valueOp)
	{
		if ((valueOp is IObjectCreationOperation oc && TryInitialize(target, oc)) || (valueOp is IConversionOperation { Operand: IObjectCreationOperation operand } && TryInitialize(target, operand)))
		{
			return;
		}
		if (valueOp is ICollectionExpressionOperation collectionExpressionOperation)
		{
			UType type = target.Type;
			UType.Array ca = type as UType.Array;
			if ((object)ca != null)
			{
				s.Emit(new EX_SetArray
				{
					AssigningProperty = target.Expr(s),
					Elements = collectionExpressionOperation.Elements.Select((IOperation e) => Coerce(Lower(e), ca.Inner, e)).ToArray()
				});
				return;
			}
		}
		Store(target, Coerce(Lower(valueOp), target.Type, valueOp));
	}

	private bool TryInitialize(Var target, IObjectCreationOperation oc)
	{
		UType type = target.Type;
		if (!(type is UType.Array array))
		{
			if (!(type is UType.Map))
			{
				if (!(type is UType.Set))
				{
					if (type is UType.Struct t)
					{
						Store(target, ZeroOf(t).Expr(s));
						foreach (IOperation item in oc.Initializer?.Initializers ?? ImmutableEmpty)
						{
							ISimpleAssignmentOperation simpleAssignmentOperation = item as ISimpleAssignmentOperation;
							bool flag;
							if (simpleAssignmentOperation != null)
							{
								IOperation target2 = simpleAssignmentOperation.Target;
								if (target2 is IFieldReferenceOperation || target2 is IPropertyReferenceOperation)
								{
									flag = true;
									goto IL_022c;
								}
							}
							flag = false;
							goto IL_022c;
							IL_022c:
							if (!flag)
							{
								throw Error(item, "unsupported struct initializer");
							}
							ISymbol member = (simpleAssignmentOperation.Target as IMemberReferenceOperation).Member;
							Assign(StructField(target, member, item), simpleAssignmentOperation.Value);
						}
						return true;
					}
					return false;
				}
				s.Emit(new EX_SetSet
				{
					SetProperty = target.Expr(s),
					Elements = Array.Empty<KismetExpression>()
				});
				IObjectOrCollectionInitializerOperation? initializer = oc.Initializer;
				if (initializer != null && initializer.Initializers.Length > 0)
				{
					throw Error(oc, "set initializers are not supported; call Add");
				}
				return true;
			}
			s.Emit(new EX_SetMap
			{
				MapProperty = target.Expr(s),
				Elements = Array.Empty<KismetExpression>()
			});
			IObjectOrCollectionInitializerOperation? initializer2 = oc.Initializer;
			if (initializer2 != null && initializer2.Initializers.Length > 0)
			{
				throw Error(oc, "dictionary initializers are not supported; call Add");
			}
			return true;
		}
		List<KismetExpression> list = new List<KismetExpression>();
		if (oc.Initializer != null)
		{
			foreach (IOperation initializer3 in oc.Initializer.Initializers)
			{
				if (!(initializer3 is IInvocationOperation invocationOperation) || invocationOperation.Arguments.Length != 1)
				{
					throw Error(initializer3, "unsupported collection initializer");
				}
				list.Add(Coerce(Lower(invocationOperation.Arguments[0].Value), array.Inner, invocationOperation));
			}
		}
		s.Emit(new EX_SetArray
		{
			AssigningProperty = target.Expr(s),
			Elements = list.ToArray()
		});
		return true;
	}

	private void Store(Var target, KismetExpression value)
	{
		s.Emit(s.Let(target.Type, target.Expr(s), value, target.Pointer(s)));
	}

	private KismetExpression Read(Value v)
	{
		if (!(v is RValue rValue))
		{
			if (v is Var var)
			{
				return var.Expr(s);
			}
			throw new InvalidOperationException();
		}
		return rValue.Expr;
	}

	private Var AsVar(Value v)
	{
		if (v is Var result)
		{
			return result;
		}
		LocalVar localVar = Temp(v.Type);
		Store(localVar, Read(v));
		return localVar;
	}

	private Var LowerTarget(IOperation op)
	{
		return (Lower(op) as Var) ?? throw Error(op, "expression is not assignable");
	}

	private Value Lower(IOperation op)
	{
		IOperation operand;
		if (op.ConstantValue.HasValue && (op.Type != null || op.ConstantValue.Value == null))
		{
			if (op is IConversionOperation conversionOperation)
			{
				operand = conversionOperation.Operand;
				if (operand != null && !operand.ConstantValue.HasValue)
				{
					goto IL_0069;
				}
			}
			return Constant(op.ConstantValue.Value, op.Type, op);
		}
		goto IL_0069;
		IL_0162:
		if (!(operand is IConditionalOperation { WhenFalse: not null } conditionalOperation))
		{
			if (!(operand is IInterpolatedStringOperation interpolatedStringOperation))
			{
				if (!(operand is IIsPatternOperation ip))
				{
					if (!(operand is IConditionalAccessOperation ca))
					{
						if (!(operand is IConditionalAccessInstanceOperation))
						{
							if (!(operand is IIsTypeOperation isTypeOperation))
							{
								if (!(operand is IObjectCreationOperation objectCreationOperation))
								{
									if (!(operand is IDelegateCreationOperation dc))
									{
										if (!(operand is IDefaultValueOperation defaultValueOperation))
										{
											if (!(operand is INameOfOperation nameOfOperation))
											{
												if (!(operand is ICoalesceOperation coalesceOperation))
												{
													if (!(operand is ITypeOfOperation))
													{
														if (!(operand is IAnonymousFunctionOperation))
														{
															if (operand is IAwaitOperation)
															{
																throw Error(op, "async/await is not supported: use timers");
															}
															throw Error(op, $"unsupported expression ({op.Kind}): {op.Syntax}");
														}
														throw Error(op, "lambdas can only be used as delegates (event handlers, delegate parameters)");
													}
													throw Error(op, "use Unreal.ClassOf<T>() instead of typeof");
												}
												UType uType = sym.Map(coalesceOperation.Type) ?? throw Error(op, "unsupported ?? type");
												LocalVar localVar = Temp(uType);
												Store(localVar, Coerce(Lower(coalesceOperation.Value), uType, coalesceOperation.Value));
												Value value = IsNull(localVar, coalesceOperation);
												if (value is RValue rValue && rValue.Expr is EX_False)
												{
													return localVar;
												}
												Label label = s.NewLabel("coalesce");
												s.JumpIfNot(Read(value), label);
												Assign(localVar, coalesceOperation.WhenNull);
												s.Mark(label);
												return localVar;
											}
											return Constant(nameOfOperation.ConstantValue.Value, nameOfOperation.Type, op);
										}
										return Default(sym.Map(defaultValueOperation.Type) ?? throw Error(op, "unsupported default"), op);
									}
									return DelegateValue(dc);
								}
								UType type = sym.Map(objectCreationOperation.Type) ?? throw Error(op, $"cannot create '{objectCreationOperation.Type}': use the SDK spawn helpers for game objects");
								LocalVar localVar2 = Temp(type);
								if (!TryInitialize(localVar2, objectCreationOperation))
								{
									throw Error(op, $"cannot create '{objectCreationOperation.Type}' here");
								}
								return localVar2;
							}
							return new RValue(IsA(TypeClassPath(isTypeOperation.TypeOperand, op), Read(Lower(isTypeOperation.ValueOperand))), UType.Bool);
						}
						if (conditionalInstances.Count <= 0)
						{
							throw Error(op, "?. without a receiver");
						}
						return conditionalInstances.Peek();
					}
					return ConditionalAccess(ca, wantValue: true);
				}
				return IsPattern(ip);
			}
			KismetExpression kismetExpression = null;
			foreach (IInterpolatedStringContentOperation part in interpolatedStringOperation.Parts)
			{
				KismetExpression kismetExpression2;
				if (!(part is IInterpolatedStringTextOperation interpolatedStringTextOperation))
				{
					if (!(part is IInterpolationOperation i))
					{
						throw Error(part, "unsupported interpolation");
					}
					kismetExpression2 = Interpolation(i);
				}
				else
				{
					kismetExpression2 = Read(Lower(interpolatedStringTextOperation.Text));
				}
				KismetExpression kismetExpression3 = kismetExpression2;
				kismetExpression = ((kismetExpression == null) ? kismetExpression3 : CallMath(LibFn("UKismetStringLibrary", "Concat_StrStr"), kismetExpression, kismetExpression3));
			}
			return new RValue(kismetExpression ?? ScriptBuilder.Str(""), UType.String);
		}
		UType type2 = sym.Map(conditionalOperation.Type) ?? throw Error(op, "unsupported conditional type");
		LocalVar localVar3 = Temp(type2);
		Label label2 = s.NewLabel("else");
		Label label3 = s.NewLabel("endif");
		Branch(conditionalOperation.Condition, label2);
		Assign(localVar3, conditionalOperation.WhenTrue);
		s.Jump(label3);
		s.Mark(label2);
		Assign(localVar3, conditionalOperation.WhenFalse);
		s.Mark(label3);
		return localVar3;
		IL_0069:
		operand = op;
		if (!(operand is IParenthesizedOperation parenthesizedOperation))
		{
			if (!(operand is ILocalReferenceOperation localReferenceOperation))
			{
				if (!(operand is IParameterReferenceOperation parameterReferenceOperation))
				{
					if (!(operand is IDeclarationExpressionOperation { Expression: ILocalReferenceOperation expression }))
					{
						if (!(operand is IInstanceReferenceOperation))
						{
							if (!(operand is IFieldReferenceOperation fieldReferenceOperation))
							{
								if (!(operand is IEventReferenceOperation er))
								{
									if (!(operand is IPropertyReferenceOperation p))
									{
										if (!(operand is IInvocationOperation inv))
										{
											if (!(operand is IConversionOperation c))
											{
												if (!(operand is IBinaryOperation b))
												{
													if (!(operand is IUnaryOperation u))
													{
														if (!(operand is ISimpleAssignmentOperation simpleAssignmentOperation))
														{
															int num;
															if (!(operand is ICompoundAssignmentOperation))
															{
																if (!(operand is IIncrementOrDecrementOperation))
																{
																	goto IL_0162;
																}
																num = 2;
															}
															else
															{
																num = 1;
															}
															if (op is IIncrementOrDecrementOperation { IsPostfix: not false })
															{
																IIncrementOrDecrementOperation incrementOrDecrementOperation2;
																if (num == 1)
																{
																	incrementOrDecrementOperation2 = operand as IIncrementOrDecrementOperation;
																	if (incrementOrDecrementOperation2 == null)
																	{
																		goto IL_0162;
																	}
																}
																else
																{
																	if (num != 2)
																	{
																		goto IL_037e;
																	}
																	incrementOrDecrementOperation2 = (IIncrementOrDecrementOperation)operand;
																}
																Var var = LowerTarget(incrementOrDecrementOperation2.Target);
																LocalVar localVar4 = Temp(var.Type);
																Store(localVar4, var.Expr(s));
																Store(var, Read(Step(localVar4, incrementOrDecrementOperation2.Kind == OperationKind.Increment, incrementOrDecrementOperation2)));
																return localVar4;
															}
															goto IL_037e;
														}
														Var var2 = LowerTarget(simpleAssignmentOperation.Target);
														Assign(var2, simpleAssignmentOperation.Value);
														return var2;
													}
													return Unary(u);
												}
												return BinaryOp(b);
											}
											return Convert(c);
										}
										return Invoke(inv, wantResult: true) ?? throw Error(op, "call has no value");
									}
									return Property(p);
								}
								return EventVar(er);
							}
							return Member(fieldReferenceOperation.Field, fieldReferenceOperation.Instance, op);
						}
						return new RValue(ScriptBuilder.Self(), new UType.Object(cls.ClassPath));
					}
					return DeclareLocal(expression.Local, op);
				}
				if (!locals.TryGetValue(parameterReferenceOperation.Parameter, out LocalVar value2))
				{
					throw Error(op, "unknown parameter " + parameterReferenceOperation.Parameter.Name);
				}
				return value2;
			}
			if (!locals.TryGetValue(localReferenceOperation.Local, out LocalVar value3))
			{
				return DeclareLocal(localReferenceOperation.Local, op);
			}
			return value3;
		}
		return Lower(parenthesizedOperation.Operand);
		IL_037e:
		Effect(op);
		return LowerTarget((op is ICompoundAssignmentOperation compoundAssignmentOperation) ? compoundAssignmentOperation.Target : ((IIncrementOrDecrementOperation)op).Target);
	}

	private Value Default(UType type, IOperation at)
	{
		if (!(type is UType.Struct t))
		{
			if (!(type is UType.Object) && !(type is UType.Class) && !(type is UType.Interface) && !(type is UType.WeakObject))
			{
				if (!(type is UType.Prim prim))
				{
					if (type is UType.Enum)
					{
						return new RValue(ScriptBuilder.Byte(0), type);
					}
					return new RValue(Read(Temp(type)), type);
				}
				string kind = prim.Kind;
				object value = ((kind == "BoolProperty") ? ((object)false) : ((!(kind == "StrProperty")) ? ((object)0) : ""));
				return Constant(value, null, at, type);
			}
			return new RValue(ScriptBuilder.NoObject(), type);
		}
		return ZeroOf(t);
	}

	private Value Constant(object? value, ITypeSymbol? csType, IOperation at, UType? forced = null)
	{
		UType uType = forced ?? ((csType != null) ? sym.Map(csType) : null);
		if (value == null)
		{
			if ((uType is UType.Object || uType is UType.Class || uType is UType.Interface || (object)uType == null) ? true : false)
			{
				return new RValue(ScriptBuilder.NoObject(), uType ?? new UType.Object("/Script/CoreUObject.Object"));
			}
			if (uType == UType.String)
			{
				return new RValue(ScriptBuilder.Str(""), uType);
			}
			throw Error(at, $"null is not a valid {uType}");
		}
		if (uType is UType.Enum)
		{
			return new RValue(ScriptBuilder.Byte(System.Convert.ToByte(value)), uType);
		}
		if (uType == UType.Name)
		{
			return new RValue(s.NameConst((string)value), uType);
		}
		if (uType == UType.Text)
		{
			return new RValue(CallMath(LibFn("UKismetTextLibrary", "Conv_StringToText"), ScriptBuilder.Str((string)value)), uType);
		}
		if (uType is UType.Prim prim)
		{
			switch (prim.Kind)
			{
			case "BoolProperty":
				return new RValue(ScriptBuilder.Bool(System.Convert.ToBoolean(value)), uType);
			case "ByteProperty":
				return new RValue(ScriptBuilder.Byte(System.Convert.ToByte(value)), uType);
			case "IntProperty":
				return new RValue(ScriptBuilder.Int(System.Convert.ToInt32(value)), uType);
			case "Int64Property":
				return new RValue(ScriptBuilder.Int64(System.Convert.ToInt64(value)), uType);
			case "UInt64Property":
				return new RValue(new EX_UInt64Const
				{
					Value = System.Convert.ToUInt64(value)
				}, uType);
			case "FloatProperty":
				return new RValue(ScriptBuilder.Float(System.Convert.ToSingle(value)), uType);
			case "DoubleProperty":
				return new RValue(ScriptBuilder.Double(System.Convert.ToDouble(value)), uType);
			case "StrProperty":
				return new RValue(ScriptBuilder.Str(System.Convert.ToString(value, CultureInfo.InvariantCulture)), uType);
			}
		}
		throw Error(at, $"constants of type {uType} are not supported");
	}

	private Value Member(IFieldSymbol field, IOperation? instance, IOperation at)
	{
		if (field.IsConst)
		{
			return Constant(field.ConstantValue, field.Type, at);
		}
		if (field.ContainingType.TypeKind == TypeKind.Enum)
		{
			return Constant(field.ConstantValue, field.Type, at);
		}
		if (field.ContainingType.TypeKind == TypeKind.Struct && sym.Property(field).HasValue)
		{
			return StructField(AsVar(Lower(instance)), field, at);
		}
		SelfMember selfMember = cls.Field(field);
		if ((object)selfMember != null)
		{
			if ((instance == null || instance is IInstanceReferenceOperation) ? true : false)
			{
				return selfMember;
			}
			return new ObjectMember(Read(Lower(instance)), selfMember.Name, selfMember.Type, cls.ImportOwnerOf(field));
		}
		if (field.IsStatic)
		{
			throw Error(at, "static fields are not supported (Blueprints have no static state): use a field on the mod actor");
		}
		throw Error(at, $"field '{field}' is not available in Blueprint");
	}

	private Var StructField(Var structVar, ISymbol member, IOperation at)
	{
		(string, UType, string) tuple = sym.Property(member) ?? throw Error(at, $"'{member}' is not a struct property");
		return new StructMember(structVar, tuple.Item1, tuple.Item2, pkg.ImportStruct(tuple.Item3));
	}

	private Value Property(IPropertyReferenceOperation p)
	{
		IPropertySymbol property = p.Property;
		if (property.IsIndexer)
		{
			return Indexer(p);
		}
		string text = Symbols.IntrinsicId(property);
		if (text != null)
		{
			return Intrinsic(text, p, p.Instance, Array.Empty<IOperation>());
		}
		INamedTypeSymbol originalDefinition = property.ContainingType.OriginalDefinition;
		if (originalDefinition != null)
		{
			if (SymbolEqualityComparer.Default.Equals(originalDefinition, sym.List) && property.Name == "Count")
			{
				return CollectionValue("Array_Length", AsVar(Lower(p.Instance)), UType.Int, p);
			}
			if (SymbolEqualityComparer.Default.Equals(originalDefinition, sym.Dictionary) && property.Name == "Count")
			{
				return CollectionValue("Map_Length", AsVar(Lower(p.Instance)), UType.Int, p);
			}
			bool flag = SymbolEqualityComparer.Default.Equals(originalDefinition, sym.Dictionary);
			if (flag)
			{
				string name = property.Name;
				bool flag2 = ((name == "Keys" || name == "Values") ? true : false);
				flag = flag2;
			}
			if (flag)
			{
				Var var = AsVar(Lower(p.Instance));
				UType.Map map = (UType.Map)var.Type;
				if (!(property.Name == "Keys"))
				{
					return CopyToArray("Map_Values", var, map.Value);
				}
				return CopyToArray("Map_Keys", var, map.Key);
			}
			if (originalDefinition.ToDisplayString() == "System.Collections.Generic.KeyValuePair<TKey, TValue>")
			{
				(LocalVar, LocalVar) value = default((LocalVar, LocalVar));
				flag = p.Instance is ILocalReferenceOperation localReferenceOperation && pairs.TryGetValue(localReferenceOperation.Local, out value);
				if (flag)
				{
					string name = property.Name;
					bool flag2 = ((name == "Key" || name == "Value") ? true : false);
					flag = flag2;
				}
				if (flag)
				{
					if (!(property.Name == "Key"))
					{
						return value.Item2;
					}
					return value.Item1;
				}
				throw Error(p, "KeyValuePair is only available as the variable of a foreach over a Dictionary (pair.Key, pair.Value)");
			}
			if (SymbolEqualityComparer.Default.Equals(originalDefinition, sym.HashSet) && property.Name == "Count")
			{
				return CollectionValue("Set_Length", AsVar(Lower(p.Instance)), UType.Int, p);
			}
			if (originalDefinition.SpecialType == SpecialType.System_String && property.Name == "Length")
			{
				return new RValue(CallMath(LibFn("UKismetStringLibrary", "Len"), Read(Lower(p.Instance))), UType.Int);
			}
		}
		if (property.ContainingType.TypeKind == TypeKind.Struct && sym.Property(property).HasValue)
		{
			return StructField(AsVar(Lower(p.Instance)), property, p);
		}
		(string, UType, string)? tuple = sym.Property(property);
		if (tuple.HasValue)
		{
			(string, UType, string) valueOrDefault = tuple.GetValueOrDefault();
			FPackageIndex owner = pkg.ImportClass(valueOrDefault.Item3);
			IOperation instance = p.Instance;
			if ((instance == null || instance is IInstanceReferenceOperation) ? true : false)
			{
				return new SelfMember(valueOrDefault.Item1, valueOrDefault.Item2, owner);
			}
			return new ObjectMember(Read(Lower(p.Instance)), valueOrDefault.Item1, valueOrDefault.Item2, owner);
		}
		SelfMember selfMember = cls.Field(property);
		if ((object)selfMember != null)
		{
			IOperation instance = p.Instance;
			if ((instance == null || instance is IInstanceReferenceOperation) ? true : false)
			{
				return selfMember;
			}
			return new ObjectMember(Read(Lower(p.Instance)), selfMember.Name, selfMember.Type, cls.ImportOwnerOf(property));
		}
		if (IsAccessorProperty(p, out IPropertyReferenceOperation pr))
		{
			return GetAccessor(pr);
		}
		throw Error(p, $"property '{property}' is not available in Blueprint");
	}

	private Value Indexer(IPropertyReferenceOperation p)
	{
		Var var = AsVar(Lower(p.Instance));
		UType type = var.Type;
		if (!(type is UType.Array array))
		{
			if (type is UType.Map map)
			{
				LocalVar localVar = Temp(map.Value);
				LocalVar localVar2 = Temp(UType.Bool);
				Effect(MapCall("Map_Find", var, localVar2, new KismetExpression[2]
				{
					RefSafe(Coerce(Lower(p.Arguments[0].Value), map.Key, p), map.Key),
					localVar.Expr(s)
				}), localVar2);
				return localVar;
			}
			if (var.Type == UType.String)
			{
				throw Error(p, "string indexing is not supported; use Substring");
			}
			throw Error(p, "unsupported indexer");
		}
		LocalVar localVar3 = Temp(array.Inner);
		Effect(ArrayCall("Array_Get", var, null, new KismetExpression[2]
		{
			Coerce(Lower(p.Arguments[0].Value), UType.Int, p),
			localVar3.Expr(s)
		}), null);
		return localVar3;
	}

	private UFunctionInfo LibFn(string className, string name)
	{
		return cls.Library(className, name) ?? throw new CompileError(className + "." + name + " was not found in the game bindings", null);
	}

	private FPackageIndex Import(UFunctionInfo f)
	{
		return pkg.ImportFunction(f.Path);
	}

	private KismetExpression CallMath(UFunctionInfo f, params KismetExpression[] args)
	{
		return s.CallMath(Import(f), RefSafeArgs(f, args));
	}

	private KismetExpression[] RefSafeArgs(UFunctionInfo f, KismetExpression[] args)
	{
		if (f.HasWildcards)
		{
			return args;
		}
		List<UParam> list = f.Inputs.ToList();
		for (int i = 0; i < args.Length && i < list.Count; i++)
		{
			if ((list[i].Flags & 0x8000100) != 0L)
			{
				args[i] = RefSafe(args[i], list[i].Type);
			}
		}
		return args;
	}

	private KismetExpression RefSafe(KismetExpression x, UType type)
	{
		bool flag = ((x is EX_LocalVariable || x is EX_LocalOutVariable || x is EX_InstanceVariable || x is EX_StructMemberContext || x is EX_IntConst || x is EX_ByteConst || x is EX_FloatConst || x is EX_DoubleConst || x is EX_Int64Const || x is EX_UInt64Const || x is EX_StringConst || x is EX_UnicodeStringConst || x is EX_NameConst || x is EX_True || x is EX_False || x is EX_NoObject || x is EX_ObjectConst || x is EX_Self) ? true : false);
		if (flag || (x is EX_Context eX_Context && eX_Context.ContextExpression is EX_InstanceVariable))
		{
			return x;
		}
		LocalVar localVar = Temp(type);
		Store(localVar, x);
		return localVar.Expr(s);
	}

	private KismetExpression CallMath(string lib, string fnName, params KismetExpression[] args)
	{
		return CallMath(LibFn(lib, fnName), args);
	}

	private Value? Invoke(IInvocationOperation inv, bool wantResult)
	{
		IMethodSymbol targetMethod = inv.TargetMethod;
		string text = Symbols.IntrinsicId(targetMethod);
		if (text != null)
		{
			return Intrinsic(text, inv, inv.Instance, (from a in inv.Arguments
				orderby a.Parameter?.Ordinal ?? 0
				select a.Value).ToList(), wantResult);
		}
		if (CollectionMethod(inv, wantResult, out Value result))
		{
			return result;
		}
		Value value = StringMethod(inv);
		if ((object)value != null)
		{
			return value;
		}
		if ((targetMethod.ContainingType.SpecialType == SpecialType.System_Object || (targetMethod.Name == "ToString" && inv.Arguments.Length == 0 && inv.Instance != null)) && targetMethod.Name == "ToString")
		{
			return new RValue(ToStringExpr(Lower(inv.Instance), inv), UType.String);
		}
		if (targetMethod.ContainingType.ToDisplayString() == "System.Math")
		{
			return MathCall(inv);
		}
		if (targetMethod.MethodKind == MethodKind.DelegateInvoke)
		{
			return RaiseEvent(inv);
		}
		UFunctionInfo uFunctionInfo = sym.Function(targetMethod);
		if ((object)uFunctionInfo != null)
		{
			return CallBound(uFunctionInfo, inv, wantResult);
		}
		if (Symbols.IsSource(targetMethod.ContainingType))
		{
			return CallSource(targetMethod, inv, wantResult);
		}
		throw Error(inv, "'" + targetMethod.ToDisplayString() + "' cannot be called from a mod (not an Unreal function)");
	}

	private UFunctionInfo? FindBound(IMethodSymbol m)
	{
		for (IMethodSymbol methodSymbol = m; methodSymbol != null; methodSymbol = methodSymbol.OverriddenMethod)
		{
			UFunctionInfo uFunctionInfo = sym.Function(methodSymbol);
			if ((object)uFunctionInfo != null)
			{
				return uFunctionInfo;
			}
		}
		return null;
	}

	private Value? CallBound(UFunctionInfo f, IInvocationOperation inv, bool wantResult)
	{
		bool throughProcessEvent = f.ScriptOnly;
		if (throughProcessEvent && (f.Return != null || f.Inputs.Any((UParam p) => p.IsOut)))
		{
			throw Error(inv, f.Path + " is implemented in AngelScript and " + ((f.Return != null) ? "returns a value" : "has out parameters") + ": mods can call AngelScript functions only when they return nothing. Read the value from a property instead");
		}
		List<KismetExpression> list = new List<KismetExpression>();
		List<UParam> list2 = f.Inputs.ToList();
		_ = inv.TargetMethod.Parameters;
		int i;
		for (i = 0; i < list2.Count; i++)
		{
			IArgumentOperation argumentOperation = inv.Arguments.FirstOrDefault(delegate(IArgumentOperation a)
			{
				IParameterSymbol? parameter = a.Parameter;
				return parameter != null && parameter.Ordinal == i;
			});
			UParam uParam = list2[i];
			if (argumentOperation == null || (argumentOperation.ArgumentKind == ArgumentKind.DefaultValue && argumentOperation.Value is IDefaultValueOperation))
			{
				list.Add(uParam.IsOut ? Read(Temp(uParam.Type)) : Read(Default(uParam.Type, inv)));
			}
			else
			{
				list.Add(uParam.IsOut ? AsVarForOut(argumentOperation.Value, uParam.Type).Expr(s) : Coerce(Lower(argumentOperation.Value), uParam.Type, argumentOperation.Value));
			}
		}
		KismetExpression[] collection = RefSafeArgs(f, list.ToArray());
		list.Clear();
		list.AddRange(collection);
		IOperation instance = inv.Instance;
		bool flag = ((instance == null || instance is IInstanceReferenceOperation) ? true : false);
		bool flag2 = flag;
		bool flag3 = instance is IInstanceReferenceOperation && !inv.IsVirtual;
		if (throughProcessEvent)
		{
			CallScript(f, f.IsStatic ? s.Object(pkg.ImportDefaultObject(f.OwnerPath)) : (flag2 ? ScriptBuilder.Self() : Read(Lower(instance))), list.ToArray());
			return null;
		}
		KismetExpression call;
		if (f.IsMathCall)
		{
			call = CallMath(f, list.ToArray());
		}
		else
		{
			KismetExpression kismetExpression = (((f.IsFinal | flag3) || f.IsStatic) ? ((KismetExpression)new EX_FinalFunction
			{
				StackNode = s.Ref(Import(f)),
				Parameters = list.ToArray()
			}) : ((KismetExpression)new EX_VirtualFunction
			{
				VirtualFunctionName = pkg.Name(f.Name),
				Parameters = list.ToArray()
			}));
			call = (f.IsStatic ? Ctx(s.Object(pkg.ImportDefaultObject(f.OwnerPath)), kismetExpression, f.Return) : ((!flag2) ? Ctx(Read(Lower(instance)), kismetExpression, f.Return) : kismetExpression));
		}
		return Finish(call, f.Return?.Type, wantResult, (!f.IsMathCall && !flag2) || (f.IsStatic && !f.IsMathCall));
	}

	/// <summary>Signature of the delegates CallScript binds: binding and adding delegates don't check it.</summary>
	private const string ScriptCallSignature = "/Script/UMG.OnButtonClickedEvent__DelegateSignature";

	/// <summary>
	/// Calls an AngelScript function. They aren't native and have no bytecode, so a bytecode call (EX_FinalFunction,
	/// EX_VirtualFunction) runs nothing and crashes the game. ProcessEvent runs them, and calling a multicast delegate goes
	/// through it: bind a delegate to the function on the target, put it in a multicast delegate and call that, with the
	/// function itself as the signature that lays out the arguments. A null target binds nothing, so nothing is called.
	/// </summary>
	private void CallScript(UFunctionInfo f, KismetExpression target, KismetExpression[] args)
	{
		LocalVar bound = Temp(new UType.Delegate(ScriptCallSignature));
		LocalVar call = Temp(new UType.MulticastDelegate(ScriptCallSignature));
		s.Emit(new EX_BindDelegate
		{
			FunctionName = pkg.Name(f.Name),
			Delegate = bound.Expr(s),
			ObjectTerm = target
		});
		// Locals keep their value between loop iterations: clear it, or the call would run once more each time.
		s.Emit(new EX_ClearMulticastDelegate
		{
			DelegateToClear = call.Expr(s)
		});
		s.Emit(new EX_AddMulticastDelegate
		{
			Delegate = call.Expr(s),
			DelegateToAdd = bound.Expr(s)
		});
		s.Emit(new EX_CallMulticastDelegate
		{
			StackNode = s.Ref(Import(f)),
			Delegate = call.Expr(s),
			Parameters = args
		});
	}

	private KismetExpression Ctx(KismetExpression obj, KismetExpression call, UParam? ret)
	{
		if (ret == null)
		{
			return s.Context(obj, call);
		}
		pendingResult = Temp(ret.Type);
		return s.Context(obj, call, pendingResult.Pointer(s));
	}

	private Value? Finish(KismetExpression call, UType? retType, bool wantResult, bool contextual)
	{
		LocalVar localVar = pendingResult;
		pendingResult = null;
		if (retType == null)
		{
			s.Emit(call);
			return null;
		}
		if (localVar != null)
		{
			Store(localVar, call);
			return localVar;
		}
		if (!wantResult)
		{
			s.Emit(call);
			return null;
		}
		if (call is EX_CallMath)
		{
			return new RValue(call, retType);
		}
		LocalVar localVar2 = Temp(retType);
		Store(localVar2, call);
		return localVar2;
	}

	private Var AsVarForOut(IOperation arg, UType type)
	{
		if (arg is IDiscardOperation)
		{
			return Temp(type);
		}
		if (Lower(arg) is Var result)
		{
			return result;
		}
		throw Error(arg, "out/ref arguments must be variables");
	}

	private Value? CallSource(IMethodSymbol m, IInvocationOperation inv, bool wantResult)
	{
		MethodPlan methodPlan = cls.Callable(m, inv);
		List<KismetExpression> list = new List<KismetExpression>();
		int i;
		for (i = 0; i < methodPlan.Params.Count; i++)
		{
			(ISymbol Symbol, string Name, UType Type, bool IsOut) tuple = methodPlan.Params[i];
			UType item = tuple.Type;
			bool item2 = tuple.IsOut;
			IOperation value = inv.Arguments.First(delegate(IArgumentOperation a)
			{
				IParameterSymbol? parameter = a.Parameter;
				return parameter != null && parameter.Ordinal == i;
			}).Value;
			list.Add(item2 ? AsVarForOut(value, item).Expr(s) : Coerce(Lower(value), item, value));
		}
		return CallSource(m, methodPlan, inv.Instance, list, wantResult);
	}

	private Value? CallSource(IMethodSymbol m, MethodPlan target, IOperation? instance, List<KismetExpression> args, bool wantResult)
	{
		bool flag = m.IsStatic;
		if (!flag)
		{
			bool flag2 = ((instance == null || instance is IInstanceReferenceOperation) ? true : false);
			flag = flag2;
		}
		bool flag3 = flag;
		UParam ret = ((target.ReturnType != null) ? new UParam(target.ReturnName, target.ReturnType, 1408uL) : null);
		KismetExpression call;
		if (flag3)
		{
			FPackageIndex index = target.Index;
			call = ((index != null && !target.External) ? ((KismetExpression)new EX_LocalFinalFunction
			{
				StackNode = index,
				Parameters = args.ToArray()
			}) : ((KismetExpression)new EX_LocalVirtualFunction
			{
				VirtualFunctionName = pkg.Name(target.Name),
				Parameters = args.ToArray()
			}));
		}
		else
		{
			call = Ctx(Read(Lower(instance)), target.ByName ? ((KismetExpression)new EX_VirtualFunction
			{
				VirtualFunctionName = pkg.Name(target.Name),
				Parameters = args.ToArray()
			}) : ((KismetExpression)new EX_FinalFunction
			{
				StackNode = s.Ref(pkg.ImportFunction(sym.ClassPath(m.ContainingType) + ":" + target.Name)),
				Parameters = args.ToArray()
			}), ret);
		}
		return Finish(call, target.ReturnType, wantResult, !flag3);
	}

	private bool IsAccessorProperty(IOperation op, out IPropertyReferenceOperation pr)
	{
		pr = op as IPropertyReferenceOperation;
		IPropertyReferenceOperation propertyReferenceOperation = pr;
		if (propertyReferenceOperation != null)
		{
			IPropertySymbol property = propertyReferenceOperation.Property;
			if (property != null && !property.IsIndexer && Symbols.IsSource(pr.Property.ContainingType) && !sym.Property(pr.Property).HasValue)
			{
				return cls.Field(pr.Property) == null;
			}
		}
		return false;
	}

	private Value GetAccessor(IPropertyReferenceOperation pr)
	{
		IMethodSymbol m = pr.Property.GetMethod ?? throw Error(pr, "property '" + pr.Property.Name + "' has no getter");
		return CallSource(m, cls.Callable(m, pr), pr.Instance, new List<KismetExpression>(), wantResult: true);
	}

	private void SetAccessor(IPropertyReferenceOperation pr, Value value)
	{
		IMethodSymbol m = pr.Property.SetMethod ?? throw Error(pr, "property '" + pr.Property.Name + "' has no setter");
		MethodPlan methodPlan = cls.Callable(m, pr);
		CallSource(m, methodPlan, pr.Instance, new List<KismetExpression> { Coerce(value, methodPlan.Params[0].Type, pr) }, wantResult: false);
	}

	private Value MathCall(IInvocationOperation inv)
	{
		string name = inv.TargetMethod.Name;
		List<Value> list = inv.Arguments.Select((IArgumentOperation a) => Lower(a.Value)).ToList();
		UType type = list[0].Type;
		string text = ((type == UType.Int) ? "int" : ((type == UType.Int64) ? "int64" : "double"));
		string text2 = name switch
		{
			"Abs" => (text == "int") ? "Abs_Int" : ((!(text == "int64")) ? "Abs" : "Abs_Int64"), 
			"Min" => (text == "int") ? "Min" : ((!(text == "int64")) ? "FMin" : "MinInt64"), 
			"Max" => (text == "int") ? "Max" : ((!(text == "int64")) ? "FMax" : "MaxInt64"), 
			"Clamp" => (text == "int") ? "Clamp" : ((!(text == "int64")) ? "FClamp" : "ClampInt64"), 
			"Sqrt" => "Sqrt", 
			"Sin" => "Sin", 
			"Cos" => "Cos", 
			"Tan" => "Tan", 
			"Pow" => "MultiplyMultiply_FloatFloat", 
			"Floor" => "FFloor64", 
			"Ceiling" => "FCeil64", 
			"Round" => "Round64", 
			"Truncate" => "FTrunc64", 
			"Exp" => "Exp", 
			"Log" => "Loge", 
			"Atan2" => "Atan2", 
			_ => null, 
		};
		if (text2 == null)
		{
			throw Error(inv, "Math." + name + " is not supported");
		}
		UFunctionInfo uFunctionInfo = LibFn("UKismetMathLibrary", text2);
		List<UParam> ps = uFunctionInfo.Inputs.ToList();
		KismetExpression expr = CallMath(uFunctionInfo, list.Select((Value a, int i) => Coerce(a, ps[i].Type, inv.Arguments[i].Value)).ToArray());
		UType type2 = uFunctionInfo.Return.Type;
		UType uType = sym.Map(inv.Type);
		return new RValue(Coerce(new RValue(expr, type2), uType, inv), uType);
	}

	private KismetExpression ArrayCall(string name, Var list, LocalVar? result, KismetExpression[] rest)
	{
		return LibraryContextCall("UKismetArrayLibrary", name, list, result, rest);
	}

	private KismetExpression MapCall(string name, Var map, LocalVar? result, KismetExpression[] rest)
	{
		return LibraryContextCall("UBlueprintMapLibrary", name, map, result, rest);
	}

	private KismetExpression SetCall(string name, Var set, LocalVar? result, KismetExpression[] rest)
	{
		return LibraryContextCall("UBlueprintSetLibrary", name, set, result, rest);
	}

	private KismetExpression LibraryContextCall(string lib, string name, Var container, LocalVar? result, KismetExpression[] rest)
	{
		UFunctionInfo uFunctionInfo = LibFn(lib, name);
		EX_FinalFunction eX_FinalFunction = new EX_FinalFunction();
		eX_FinalFunction.StackNode = s.Ref(Import(uFunctionInfo));
		eX_FinalFunction.Parameters = new KismetExpression[1] { container.Expr(s) }.Concat(rest).ToArray();
		EX_FinalFunction inner = eX_FinalFunction;
		EX_Context eX_Context = s.Context(s.Object(pkg.ImportDefaultObject(uFunctionInfo.OwnerPath)), inner, result?.Pointer(s));
		if (!(result != null))
		{
			return eX_Context;
		}
		return s.Let(result.Type, result.Expr(s), eX_Context, result.Pointer(s));
	}

	private void Effect(KismetExpression statement, LocalVar? _)
	{
		s.Emit(statement);
	}

	private Value CollectionValue(string fnName, Var container, UType type, IOperation at)
	{
		LocalVar result = Temp(type);
		string lib = (fnName.StartsWith("Array") ? "UKismetArrayLibrary" : (fnName.StartsWith("Map") ? "UBlueprintMapLibrary" : "UBlueprintSetLibrary"));
		s.Emit(LibraryContextCall(lib, fnName, container, result, Array.Empty<KismetExpression>()));
		return result;
	}

	private bool CollectionMethod(IInvocationOperation inv, bool wantResult, out Value? result)
	{
		result = null;
		INamedTypeSymbol originalDefinition = inv.TargetMethod.ContainingType.OriginalDefinition;
		bool flag = SymbolEqualityComparer.Default.Equals(originalDefinition, sym.List);
		bool flag2 = SymbolEqualityComparer.Default.Equals(originalDefinition, sym.Dictionary);
		bool flag3 = SymbolEqualityComparer.Default.Equals(originalDefinition, sym.HashSet);
		if (!flag && !flag2 && !flag3)
		{
			return false;
		}
		Var var = AsVar(Lower(inv.Instance));
		string name = inv.TargetMethod.Name;
		LocalVar ret = null;
		if (flag)
		{
			UType inner = ((UType.Array)var.Type).Inner;
			ScriptBuilder scriptBuilder = s;
			scriptBuilder.Emit(name switch
			{
				"Add" => ArrayCall("Array_Add", var, null, new KismetExpression[1] { arg(0, inner) }), 
				"Clear" => ArrayCall("Array_Clear", var, null, Array.Empty<KismetExpression>()), 
				"RemoveAt" => ArrayCall("Array_Remove", var, null, new KismetExpression[1] { arg(0, UType.Int) }), 
				"Remove" => ArrayCall("Array_RemoveItem", var, R(UType.Bool), new KismetExpression[1] { arg(0, inner) }), 
				"Contains" => ArrayCall("Array_Contains", var, R(UType.Bool), new KismetExpression[1] { arg(0, inner) }), 
				"IndexOf" => ArrayCall("Array_Find", var, R(UType.Int), new KismetExpression[1] { arg(0, inner) }), 
				"Insert" => ArrayCall("Array_Insert", var, null, new KismetExpression[2]
				{
					arg(1, inner),
					arg(0, UType.Int)
				}), 
				_ => throw Error(inv, "List." + name + " is not supported"), 
			});
		}
		else if (flag2)
		{
			UType.Map map = (UType.Map)var.Type;
			switch (name)
			{
			case "Add":
				s.Emit(MapCall("Map_Add", var, null, new KismetExpression[2]
				{
					arg(0, map.Key),
					arg(1, map.Value)
				}));
				break;
			case "Remove":
				s.Emit(MapCall("Map_Remove", var, R(UType.Bool), new KismetExpression[1] { arg(0, map.Key) }));
				break;
			case "ContainsKey":
				s.Emit(MapCall("Map_Contains", var, R(UType.Bool), new KismetExpression[1] { arg(0, map.Key) }));
				break;
			case "Clear":
				s.Emit(MapCall("Map_Clear", var, null, Array.Empty<KismetExpression>()));
				break;
			case "TryGetValue":
				s.Emit(MapCall("Map_Find", var, R(UType.Bool), new KismetExpression[2]
				{
					arg(0, map.Key),
					AsVarForOut(inv.Arguments[1].Value, map.Value).Expr(s)
				}));
				break;
			default:
				throw Error(inv, "Dictionary." + name + " is not supported");
			}
		}
		else
		{
			UType element = ((UType.Set)var.Type).Element;
			ScriptBuilder scriptBuilder = s;
			scriptBuilder.Emit(name switch
			{
				"Add" => SetCall("Set_Add", var, null, new KismetExpression[1] { arg(0, element) }), 
				"Remove" => SetCall("Set_Remove", var, R(UType.Bool), new KismetExpression[1] { arg(0, element) }), 
				"Contains" => SetCall("Set_Contains", var, R(UType.Bool), new KismetExpression[1] { arg(0, element) }), 
				"Clear" => SetCall("Set_Clear", var, null, Array.Empty<KismetExpression>()), 
				_ => throw Error(inv, "HashSet." + name + " is not supported"), 
			});
			if ((name == "Add") & wantResult)
			{
				result = new RValue(ScriptBuilder.Bool(v: true), UType.Bool);
				return true;
			}
		}
		result = ret;
		return true;
		LocalVar R(UType t)
		{
			return ret = Temp(t);
		}
		KismetExpression arg(int i, UType t)
		{
			return RefSafe(Coerce(Lower(inv.Arguments[i].Value), t, inv.Arguments[i].Value), t);
		}
	}

	private Value? StringMethod(IInvocationOperation inv)
	{
		IMethodSymbol targetMethod = inv.TargetMethod;
		if (targetMethod.ContainingType.SpecialType != SpecialType.System_String)
		{
			return null;
		}
		string name = targetMethod.Name;
		int length = inv.Arguments.Length;
		KismetExpression kismetExpression;
		switch (name)
		{
		case "Contains":
			if (length != 1)
			{
				goto default;
			}
			kismetExpression = str("Contains", new KismetExpression[4]
			{
				self(),
				arg(0),
				ScriptBuilder.Bool(v: true),
				ScriptBuilder.Bool(v: false)
			});
			break;
		case "StartsWith":
			if (length != 1)
			{
				goto default;
			}
			kismetExpression = str("StartsWith", new KismetExpression[3]
			{
				self(),
				arg(0),
				ScriptBuilder.Byte(0)
			});
			break;
		case "EndsWith":
			if (length != 1)
			{
				goto default;
			}
			kismetExpression = str("EndsWith", new KismetExpression[3]
			{
				self(),
				arg(0),
				ScriptBuilder.Byte(0)
			});
			break;
		case "ToUpper":
		case "ToUpperInvariant":
			if (length != 0)
			{
				goto default;
			}
			kismetExpression = str("ToUpper", new KismetExpression[1] { self() });
			break;
		case "ToLower":
		case "ToLowerInvariant":
			if (length != 0)
			{
				goto default;
			}
			kismetExpression = str("ToLower", new KismetExpression[1] { self() });
			break;
		case "Trim":
			if (length != 0)
			{
				goto default;
			}
			kismetExpression = str("TrimTrailing", new KismetExpression[1] { str("Trim", new KismetExpression[1] { self() }) });
			break;
		case "Substring":
			if (length != 1)
			{
				if (length != 2)
				{
					goto default;
				}
				kismetExpression = str("Mid", new KismetExpression[3]
				{
					self(),
					arg(0),
					arg(1)
				});
				break;
			}
			kismetExpression = str("RightChop", new KismetExpression[2]
			{
				self(),
				arg(0)
			});
			break;
		case "IndexOf":
			if (length != 1)
			{
				goto default;
			}
			kismetExpression = str("FindSubstring", new KismetExpression[5]
			{
				self(),
				arg(0),
				ScriptBuilder.Bool(v: true),
				ScriptBuilder.Bool(v: false),
				ScriptBuilder.Int(-1)
			});
			break;
		case "Replace":
			if (length != 2)
			{
				goto default;
			}
			kismetExpression = str("Replace", new KismetExpression[4]
			{
				self(),
				arg(0),
				arg(1),
				ScriptBuilder.Byte(0)
			});
			break;
		case "Equals":
			if (length != 1)
			{
				goto default;
			}
			kismetExpression = str("EqualEqual_StrStr", new KismetExpression[2]
			{
				self(),
				arg(0)
			});
			break;
		case "IsNullOrEmpty":
			if (length != 1)
			{
				goto default;
			}
			kismetExpression = str("IsEmpty", new KismetExpression[1] { arg(0) });
			break;
		case "Concat":
			if (length != 2)
			{
				goto default;
			}
			kismetExpression = str("Concat_StrStr", new KismetExpression[2]
			{
				arg(0),
				arg(1)
			});
			break;
		case "ToString":
			if (length != 0)
			{
				goto default;
			}
			kismetExpression = self();
			break;
		default:
			throw Error(inv, "string." + targetMethod.Name + " is not supported");
		}
		KismetExpression expr = kismetExpression;
		UType type = sym.Map(targetMethod.ReturnType) ?? UType.String;
		return new RValue(expr, type);
		KismetExpression arg(int i)
		{
			return Read(Lower(inv.Arguments[i].Value));
		}
		KismetExpression self()
		{
			return Read(Lower(inv.Instance));
		}
		KismetExpression str(string fnName, params KismetExpression[] a)
		{
			return CallMath("UKismetStringLibrary", fnName, a);
		}
	}

	private KismetExpression Interpolation(IInterpolationOperation i)
	{
		if (i.Alignment != null)
		{
			throw Error(i, "alignment in interpolated strings ({x,10}) is not supported");
		}
		Value value = Lower(i.Expression);
		string text = (i.FormatString?.ConstantValue.Value as string) ?? ((i.FormatString as IInterpolatedStringTextOperation)?.Text.ConstantValue.Value as string);
		if (text == null)
		{
			return ToStringExpr(value, i);
		}
		bool v = false;
		Match match = Regex.Match(text, "^0(\\.(0*)(#*))?$");
		Match match2 = Regex.Match(text, "^([FfNn])(\\d?)$");
		int num;
		int v2;
		bool flag;
		if (match.Success)
		{
			num = match.Groups[2].Value.Length;
			v2 = num + match.Groups[3].Value.Length;
		}
		else
		{
			if (!match2.Success)
			{
				throw Error(i, "format \"" + text + "\" is not supported; use 0, 0.0, 0.00, 0.##, F2 or N1");
			}
			num = (v2 = ((match2.Groups[2].Value.Length > 0) ? int.Parse(match2.Groups[2].Value) : 2));
			string value2 = match2.Groups[1].Value;
			flag = ((value2 == "N" || value2 == "n") ? true : false);
			v = flag;
		}
		if (!(value.Type is UType.Prim prim))
		{
			goto IL_01d3;
		}
		switch (prim.Kind)
		{
		case "IntProperty":
		case "Int64Property":
		case "FloatProperty":
		case "DoubleProperty":
		case "ByteProperty":
			break;
		default:
			goto IL_01d3;
		}
		flag = true;
		goto IL_01d6;
		IL_01d6:
		if (!flag)
		{
			throw Error(i, $"number formats need a number, not {value.Type}");
		}
		KismetExpression kismetExpression = CallMath("UKismetTextLibrary", "Conv_DoubleToText", Coerce(value, UType.Double, i), ScriptBuilder.Byte(1), ScriptBuilder.Bool(v: false), ScriptBuilder.Bool(v), ScriptBuilder.Int(1), ScriptBuilder.Int(324), ScriptBuilder.Int(num), ScriptBuilder.Int(v2));
		return CallMath("UKismetTextLibrary", "Conv_TextToString", kismetExpression);
		IL_01d3:
		flag = false;
		goto IL_01d6;
	}

	private KismetExpression ToStringExpr(Value v, IOperation at)
	{
		KismetExpression kismetExpression = Read(v);
		UType type = v.Type;
		if (type is UType.Prim prim)
		{
			switch (prim.Kind)
			{
			case "StrProperty":
				return kismetExpression;
			case "IntProperty":
				return CallMath("UKismetStringLibrary", "Conv_IntToString", kismetExpression);
			case "Int64Property":
				return CallMath("UKismetStringLibrary", "Conv_Int64ToString", kismetExpression);
			case "DoubleProperty":
				return CallMath("UKismetStringLibrary", "Conv_DoubleToString", kismetExpression);
			case "FloatProperty":
				return CallMath("UKismetStringLibrary", "Conv_DoubleToString", CallMath("UKismetMathLibrary", "Conv_FloatToDouble", kismetExpression));
			case "BoolProperty":
				return CallMath("UKismetStringLibrary", "Conv_BoolToString", kismetExpression);
			case "ByteProperty":
				break;
			case "NameProperty":
				return CallMath("UKismetStringLibrary", "Conv_NameToString", kismetExpression);
			case "TextProperty":
				return CallMath("UKismetTextLibrary", "Conv_TextToString", kismetExpression);
			default:
				goto IL_0364;
			}
		}
		else if (!(type is UType.Enum))
		{
			if (!(type is UType.Object) && !(type is UType.Class))
			{
				if (type is UType.Struct obj)
				{
					string structPath = obj.StructPath;
					if (structPath == "/Script/CoreUObject.Vector")
					{
						return CallMath("UKismetStringLibrary", "Conv_VectorToString", kismetExpression);
					}
					if (structPath == "/Script/CoreUObject.Rotator")
					{
						return CallMath("UKismetStringLibrary", "Conv_RotatorToString", kismetExpression);
					}
				}
				goto IL_0364;
			}
			return CallMath("UKismetStringLibrary", "Conv_ObjectToString", kismetExpression);
		}
		return CallMath("UKismetStringLibrary", "Conv_ByteToString", kismetExpression);
		IL_0364:
		throw Error(at, $"cannot convert {v.Type} to string");
	}

	private Value? Intrinsic(string id, IOperation op, IOperation? instance, IReadOnlyList<IOperation> args, bool wantResult = true)
	{
		IMethodSymbol methodSymbol = (op as IInvocationOperation)?.TargetMethod;
		switch (id)
		{
		case "name.from-string":
			if (!(args[0].ConstantValue.Value is string v))
			{
				return new RValue(CallMath("UKismetStringLibrary", "Conv_StringToName", Read(arg(0))), UType.Name);
			}
			return new RValue(s.NameConst(v), UType.Name);
		case "name.to-string":
			return new RValue(CallMath("UKismetStringLibrary", "Conv_NameToString", Read(Lower(instance))), UType.String);
		case "name.equals":
			return new RValue(CallMath("UKismetMathLibrary", "EqualEqual_NameName", Read(arg(0)), Read(arg(1))), UType.Bool);
		case "name.not-equals":
			return new RValue(CallMath("UKismetMathLibrary", "NotEqual_NameName", Read(arg(0)), Read(arg(1))), UType.Bool);
		case "name.none":
			return new RValue(s.NameConst("None"), UType.Name);
		case "text.from-string":
			return new RValue(CallMath("UKismetTextLibrary", "Conv_StringToText", Read(arg(0))), UType.Text);
		case "text.to-string":
			return new RValue(CallMath("UKismetTextLibrary", "Conv_TextToString", Read(Lower(instance))), UType.String);
		case "mod-name":
			return new RValue(ScriptBuilder.Str(cls.Mod.ModName), UType.String);
		case "subclass.null":
			return new RValue(ScriptBuilder.NoObject(), sym.Map(op.Type));
		case "class-of":
		{
			ITypeSymbol t3 = methodSymbol.TypeArguments[0];
			return new RValue(s.Object(ClassImport(t3, op)), new UType.Class(sym.ClassPath(t3)));
		}
		case "object-at":
		{
			string objectPath = (args[0].ConstantValue.Value as string) ?? throw Error(op, "Unreal.ObjectAt needs a constant path");
			ITypeSymbol t2 = methodSymbol.TypeArguments[0];
			return new RValue(s.Object(pkg.ImportObject(objectPath, sym.ClassPath(t2))), new UType.Object(sym.ClassPath(t2)));
		}
		case "class-at":
		{
			string classPath = (args[0].ConstantValue.Value as string) ?? throw Error(op, "Unreal.ClassAt needs a constant path");
			ITypeSymbol t = methodSymbol.TypeArguments[0];
			return new RValue(s.Object(pkg.ImportClass(classPath)), new UType.Class(sym.ClassPath(t)));
		}
		case "load-class":
		{
			string text = sym.ClassPath(methodSymbol.TypeArguments[0]) ?? throw Error(op, "Unreal.LoadClass needs a class type");
			UType.Class type2 = new UType.Class(text);
			UType.Struct type3 = new UType.Struct("/Script/CoreUObject.SoftClassPath", 32);
			KismetExpression kismetExpression3 = RefSafe(CallMath("UKismetSystemLibrary", "MakeSoftClassPath", Coerce(arg(0), UType.String, args[0])), type3);
			LocalVar localVar2 = Temp(type2);
			Store(localVar2, CallMath("UKismetSystemLibrary", "LoadClassAsset_Blocking", CallMath("UKismetSystemLibrary", "Conv_SoftClassPathToSoftClassRef", kismetExpression3)));
			Label label = s.NewLabel("loadclass_null");
			Label label2 = s.NewLabel("loadclass_end");
			s.JumpIfNot(CallMath("UKismetMathLibrary", "ClassIsChildOf", localVar2.Expr(s), s.Object(pkg.ImportClass(text))), label);
			s.Jump(label2);
			s.Mark(label);
			Store(localVar2, ScriptBuilder.NoObject());
			s.Mark(label2);
			return localVar2;
		}
		case "datatable-row":
		{
			UType type = sym.Map(methodSymbol.TypeArguments[0]) ?? throw Error(op, "DataTableRow needs a game struct type");
			KismetExpression kismetExpression = Read(Lower(StripObject(args[0])));
			KismetExpression kismetExpression2 = Coerce(arg(1), UType.Name, args[1]);
			Var var2 = AsVarForOut(args[2], type);
			UFunctionInfo uFunctionInfo = LibFn("UDataTableFunctionLibrary", "GetDataTableRowFromName");
			LocalVar localVar = Temp(UType.Bool);
			EX_FinalFunction eX_FinalFunction = new EX_FinalFunction();
			eX_FinalFunction.StackNode = s.Ref(Import(uFunctionInfo));
			eX_FinalFunction.Parameters = new KismetExpression[3]
			{
				kismetExpression,
				kismetExpression2,
				var2.Expr(s)
			};
			EX_FinalFunction inner = eX_FinalFunction;
			Store(localVar, s.Context(s.Object(pkg.ImportDefaultObject(uFunctionInfo.OwnerPath)), inner, localVar.Pointer(s)));
			return localVar;
		}
		case "weak.get":
		{
			Var var = AsVar(Lower(instance));
			return new RValue(Read(var), new UType.Object(((UType.WeakObject)var.Type).ClassPath));
		}
		case "weak.from":
			return new RValue(Read(arg(0)), sym.Map(op.Type));
		default:
			throw Error(op, "intrinsic '" + id + "' is not implemented");
		}
		Value arg(int i)
		{
			return Lower(args[i]);
		}
	}

	private void EventAssign(IEventAssignmentOperation ev)
	{
		if (!(ev.EventReference is IEventReferenceOperation er))
		{
			throw Error(ev, "unsupported event");
		}
		Var var = EventVar(er);
		Value v = DelegateValue((ev.HandlerValue as IDelegateCreationOperation) ?? ((ev.HandlerValue as IConversionOperation)?.Operand as IDelegateCreationOperation) ?? throw Error(ev, "event handlers must be methods of the mod class"));
		s.Emit(ev.Adds ? ((KismetExpression)new EX_AddMulticastDelegate
		{
			Delegate = var.Expr(s),
			DelegateToAdd = Read(v)
		}) : ((KismetExpression)new EX_RemoveMulticastDelegate
		{
			Delegate = var.Expr(s),
			DelegateToAdd = Read(v)
		}));
	}

	private Var EventVar(IEventReferenceOperation er)
	{
		IOperation instance = er.Instance;
		bool flag = ((instance == null || instance is IInstanceReferenceOperation) ? true : false);
		bool flag2 = flag;
		(string, UType, string)? tuple = sym.Property(er.Event);
		if (tuple.HasValue)
		{
			(string, UType, string) valueOrDefault = tuple.GetValueOrDefault();
			FPackageIndex owner = pkg.ImportClass(valueOrDefault.Item3);
			if (!flag2)
			{
				return new ObjectMember(Read(Lower(er.Instance)), valueOrDefault.Item1, valueOrDefault.Item2, owner);
			}
			return new SelfMember(valueOrDefault.Item1, valueOrDefault.Item2, owner);
		}
		SelfMember selfMember = cls.Field(er.Event) ?? throw Error(er, "event '" + er.Event.Name + "' is not an Unreal delegate");
		if (!flag2)
		{
			return new ObjectMember(Read(Lower(er.Instance)), selfMember.Name, selfMember.Type, cls.ImportOwnerOf(er.Event));
		}
		return selfMember;
	}

	private Value? RaiseEvent(IInvocationOperation inv)
	{
		Var var = AsVar(Lower(inv.Instance));
		if (!(var.Type is UType.MulticastDelegate multicastDelegate))
		{
			throw Error(inv, "only events can be invoked (single delegates can't be called from a mod)");
		}
		ImmutableArray<IParameterSymbol> parameters = inv.TargetMethod.Parameters;
		KismetExpression[] array = new KismetExpression[parameters.Length];
		int i;
		for (i = 0; i < parameters.Length; i++)
		{
			UType uType = sym.Map(parameters[i].Type) ?? throw Error(inv, $"parameter type '{parameters[i].Type}' has no Unreal equivalent");
			IOperation value = inv.Arguments.First(delegate(IArgumentOperation a)
			{
				IParameterSymbol? parameter = a.Parameter;
				return parameter != null && parameter.Ordinal == i;
			}).Value;
			array[i] = RefSafe(Coerce(Lower(value), uType, value), uType);
		}
		s.Emit(new EX_CallMulticastDelegate
		{
			StackNode = s.Ref(pkg.ImportFunction(multicastDelegate.SignaturePath, delegateSignature: true)),
			Delegate = var.Expr(s),
			Parameters = array
		});
		return null;
	}

	private Value DelegateValue(IDelegateCreationOperation dc)
	{
		if (dc.Target is IAnonymousFunctionOperation anonymousFunctionOperation)
		{
			CheckNoCaptures(anonymousFunctionOperation.Symbol, anonymousFunctionOperation.Body);
			MethodPlan methodPlan = cls.Callable(anonymousFunctionOperation.Symbol, dc);
			return new RValue(new EX_InstanceDelegate
			{
				FunctionName = pkg.Name(methodPlan.Name)
			}, sym.Map(dc.Type) ?? new UType.Delegate("?"));
		}
		if (!(dc.Target is IMethodReferenceOperation methodReferenceOperation))
		{
			throw Error(dc, "delegates must reference a method or a lambda");
		}
		IOperation instance = methodReferenceOperation.Instance;
		if ((instance != null && !(instance is IInstanceReferenceOperation)) || 1 == 0)
		{
			throw Error(dc, "delegates can only bind methods of the mod class itself");
		}
		MethodPlan methodPlan2 = cls.Callable(methodReferenceOperation.Method, dc);
		return new RValue(new EX_InstanceDelegate
		{
			FunctionName = pkg.Name(methodPlan2.Name)
		}, sym.Map(dc.Type) ?? new UType.Delegate("?"));
	}

	private static void CheckNoCaptures(IMethodSymbol fn, IOperation body)
	{
		foreach (IOperation item in body.DescendantsAndSelf())
		{
			ISymbol symbol = ((item is ILocalReferenceOperation localReferenceOperation) ? ((ISymbol)localReferenceOperation.Local) : ((ISymbol)((!(item is IParameterReferenceOperation parameterReferenceOperation)) ? null : parameterReferenceOperation.Parameter)));
			ISymbol symbol2 = symbol;
			if (symbol2 == null)
			{
				continue;
			}
			ISymbol containingSymbol = symbol2.ContainingSymbol;
			while (true)
			{
				if (containingSymbol != null)
				{
					if (SymbolEqualityComparer.Default.Equals(containingSymbol, fn))
					{
						break;
					}
					containingSymbol = containingSymbol.ContainingSymbol;
					continue;
				}
				throw new CompileError("'" + symbol2.Name + "' belongs to the enclosing method: lambdas and local functions can't capture its locals or parameters (Blueprint functions have no closures). Store it in a field instead.", item.Syntax.GetLocation());
			}
		}
	}

	private Value BinaryOp(IBinaryOperation b)
	{
		if (b.OperatorMethod != null)
		{
			string text = Symbols.IntrinsicId(b.OperatorMethod);
			if (text != null)
			{
				return Intrinsic(text, b, null, new IOperation[2] { b.LeftOperand, b.RightOperand });
			}
		}
		BinaryOperatorKind operatorKind = b.OperatorKind;
		if ((uint)(operatorKind - 13) <= 1u)
		{
			bool flag = b.OperatorKind == BinaryOperatorKind.ConditionalAnd;
			LocalVar localVar = Temp(UType.Bool);
			Store(localVar, Bool(b.LeftOperand));
			Label label = s.NewLabel(flag ? "and" : "or");
			s.JumpIfNot(flag ? localVar.Expr(s) : CallMath("UKismetMathLibrary", "Not_PreBool", localVar.Expr(s)), label);
			Store(localVar, Bool(b.RightOperand));
			s.Mark(label);
			return localVar;
		}
		operatorKind = b.OperatorKind;
		bool flag2 = ((operatorKind == BinaryOperatorKind.Equals || operatorKind == BinaryOperatorKind.NotEquals) ? true : false);
		if (flag2 && (IsNullLiteral(b.LeftOperand) || IsNullLiteral(b.RightOperand)))
		{
			IOperation op = (IsNullLiteral(b.LeftOperand) ? b.RightOperand : b.LeftOperand);
			Value value = IsNull(Lower(StripObject(op)), b);
			if (b.OperatorKind != BinaryOperatorKind.Equals)
			{
				return new RValue(CallMath("UKismetMathLibrary", "Not_PreBool", Read(value)), UType.Bool);
			}
			return value;
		}
		Value value2 = Lower(StripObject(b.LeftOperand));
		Value value3 = Lower(StripObject(b.RightOperand));
		ITypeSymbol? type = b.Type;
		if (type != null && type.SpecialType == SpecialType.System_String && b.OperatorKind == BinaryOperatorKind.Add)
		{
			return new RValue(CallMath("UKismetStringLibrary", "Concat_StrStr", ToStringExpr(value2, b.LeftOperand), ToStringExpr(value3, b.RightOperand)), UType.String);
		}
		switch (b.OperatorKind)
		{
		case BinaryOperatorKind.Equals:
		case BinaryOperatorKind.NotEquals:
		case BinaryOperatorKind.LessThan:
		case BinaryOperatorKind.LessThanOrEqual:
		case BinaryOperatorKind.GreaterThanOrEqual:
		case BinaryOperatorKind.GreaterThan:
			flag2 = true;
			break;
		default:
			flag2 = false;
			break;
		}
		if (flag2)
		{
			return Compare(b.OperatorKind, value2, value3, b);
		}
		UType type2 = sym.Map(b.Type) ?? throw Error(b, $"unsupported operand type {b.Type}");
		return Binary(b.OperatorKind, value2, value3, type2, b);
	}

	private static bool IsNullLiteral(IOperation op)
	{
		Optional<object> constantValue = op.ConstantValue;
		if (constantValue.HasValue)
		{
			return constantValue.Value == null;
		}
		return false;
	}

	private Value IsNull(Value value, IOperation at)
	{
		UType type = value.Type;
		if (!(type is UType.Object) && !(type is UType.Class) && !(type is UType.Interface))
		{
			if (!(type is UType.SoftObject))
			{
				if (!(type is UType.SoftClass))
				{
					if (type is UType.WeakObject)
					{
						throw Error(at, "check a weak reference with .Get() == null");
					}
					if (!(value is Var))
					{
						Store(Temp(value.Type), Read(value));
					}
					return new RValue(ScriptBuilder.Bool(v: false), UType.Bool);
				}
				return new RValue(CallMath("UKismetMathLibrary", "Not_PreBool", CallMath("UKismetSystemLibrary", "IsValidSoftClassReference", Read(value))), UType.Bool);
			}
			return new RValue(CallMath("UKismetMathLibrary", "Not_PreBool", CallMath("UKismetSystemLibrary", "IsValidSoftObjectReference", Read(value))), UType.Bool);
		}
		return new RValue(CallMath("UKismetMathLibrary", "EqualEqual_ObjectObject", Read(value), ScriptBuilder.NoObject()), UType.Bool);
	}

	private static IOperation StripObject(IOperation op)
	{
		while (op is IConversionOperation conversionOperation)
		{
			ITypeSymbol type = op.Type;
			if (type == null || type.SpecialType != SpecialType.System_Object)
			{
				break;
			}
			op = conversionOperation.Operand;
		}
		return op;
	}

	private Value Compare(BinaryOperatorKind kind, Value left, Value right, IOperation at)
	{
		string text = kind switch
		{
			BinaryOperatorKind.Equals => "EqualEqual", 
			BinaryOperatorKind.NotEquals => "NotEqual", 
			BinaryOperatorKind.LessThan => "Less", 
			BinaryOperatorKind.LessThanOrEqual => "LessEqual", 
			BinaryOperatorKind.GreaterThan => "Greater", 
			_ => "GreaterEqual", 
		};
		UType type = left.Type;
		UType uType;
		if ((type is UType.Object || type is UType.Class) ? true : false)
		{
			uType = left.Type;
		}
		else
		{
			UType type2 = right.Type;
			bool flag = ((type2 is UType.Object || type2 is UType.Class) ? true : false);
			uType = (flag ? right.Type : Wider(left.Type, right.Type));
		}
		UType uType2 = uType;
		string className = "UKismetMathLibrary";
		string text3;
		if (!(uType2 is UType.Object) && !(uType2 is UType.Interface))
		{
			if (!(uType2 is UType.Class))
			{
				if (!(uType2 is UType.Enum))
				{
					if (!(uType2 is UType.Prim prim))
					{
						throw Error(at, $"cannot compare values of type {uType2}");
					}
					string text2;
					switch (prim.Kind)
					{
					case "IntProperty":
						text2 = "IntInt";
						break;
					case "Int64Property":
						text2 = "Int64Int64";
						break;
					case "ByteProperty":
						text2 = "ByteByte";
						break;
					case "FloatProperty":
					case "DoubleProperty":
						text2 = "DoubleDouble";
						break;
					case "BoolProperty":
						text2 = "BoolBool";
						break;
					case "NameProperty":
						text2 = "NameName";
						break;
					case "StrProperty":
						text2 = "StrStr";
						break;
					case "UInt64Property":
						if ((kind == BinaryOperatorKind.Equals || kind == BinaryOperatorKind.NotEquals) ? true : false)
						{
							text2 = "Int64Int64";
							break;
						}
						goto default;
					default:
						throw Error(at, $"cannot compare {uType2} with {text}");
					}
					text3 = text2;
					if (prim.Kind == "StrProperty")
					{
						className = "UKismetStringLibrary";
					}
					if (prim.Kind == "FloatProperty")
					{
						uType2 = UType.Double;
					}
				}
				else
				{
					text3 = "ByteByte";
				}
			}
			else
			{
				text3 = "ClassClass";
			}
		}
		else
		{
			text3 = "ObjectObject";
		}
		UFunctionInfo uFunctionInfo = LibFn(className, text + "_" + text3);
		List<UParam> list = uFunctionInfo.Inputs.ToList();
		return new RValue(CallMath(uFunctionInfo, Coerce(left, list[0].Type, at), Coerce(right, list[1].Type, at)), UType.Bool);
	}

	private static UType Wider(UType a, UType b)
	{
		if (rank(a) < rank(b))
		{
			return b;
		}
		return a;
		static int rank(UType t)
		{
			if (!(t == UType.Double))
			{
				if (!(t == UType.Float))
				{
					if (!(t == UType.Int64))
					{
						if (!(t == UType.Int))
						{
							return (t == UType.Byte) ? 1 : 0;
						}
						return 2;
					}
					return 3;
				}
				return 4;
			}
			return 5;
		}
	}

	private Value Binary(BinaryOperatorKind kind, Value left, Value right, UType type, IOperation at)
	{
		if (type == UType.Bool)
		{
			return new RValue(CallMath("UKismetMathLibrary", kind switch
			{
				BinaryOperatorKind.And => "BooleanAND", 
				BinaryOperatorKind.Or => "BooleanOR", 
				BinaryOperatorKind.ExclusiveOr => "BooleanXOR", 
				_ => throw Error(at, $"operator {kind} on bool"), 
			}, Read(left), Read(right)), UType.Bool);
		}
		if (type == UType.String && kind == BinaryOperatorKind.Add)
		{
			return new RValue(CallMath("UKismetStringLibrary", "Concat_StrStr", ToStringExpr(left, at), ToStringExpr(right, at)), UType.String);
		}
		if ((uint)(kind - 8) <= 1u)
		{
			return Shift(kind == BinaryOperatorKind.LeftShift, left, right, type, at);
		}
		if (kind == BinaryOperatorKind.UnsignedRightShift)
		{
			throw Error(at, ">>> is not supported: use >> (on a non-negative value it gives the same result)");
		}
		UType uType = ((type == UType.Float) ? UType.Double : type);
		object obj;
		if (!(uType == UType.Int))
		{
			if (!(uType == UType.Int64))
			{
				if (!(uType == UType.Double))
				{
					if (!(uType == UType.Byte))
					{
						throw Error(at, $"arithmetic on {type} is not supported");
					}
					obj = "ByteByte";
				}
				else
				{
					obj = "DoubleDouble";
				}
			}
			else
			{
				obj = "Int64Int64";
			}
		}
		else
		{
			obj = "IntInt";
		}
		string text = (string)obj;
		string text2 = kind switch
		{
			BinaryOperatorKind.Add => "Add", 
			BinaryOperatorKind.Subtract => "Subtract", 
			BinaryOperatorKind.Multiply => "Multiply", 
			BinaryOperatorKind.Divide => "Divide", 
			BinaryOperatorKind.Remainder => "Percent", 
			BinaryOperatorKind.And => "And", 
			BinaryOperatorKind.Or => "Or", 
			BinaryOperatorKind.ExclusiveOr => "Xor", 
			_ => throw Error(at, $"operator {kind} is not supported"), 
		};
		if (text2 == "Percent" && text == "DoubleDouble")
		{
			text = "FloatFloat";
		}
		UFunctionInfo uFunctionInfo = LibFn("UKismetMathLibrary", text2 + "_" + text);
		List<UParam> list = uFunctionInfo.Inputs.ToList();
		RValue rValue = new RValue(CallMath(uFunctionInfo, Coerce(left, list[0].Type, at), Coerce(right, list[1].Type, at)), uFunctionInfo.Return.Type);
		if (!(type == UType.Float))
		{
			return rValue;
		}
		return new RValue(Coerce(rValue, UType.Float, at), UType.Float);
	}

	private Value Shift(bool left, Value value, Value count, UType type, IOperation at)
	{
		bool wide = type == UType.Int64;
		if (type != UType.Int && !wide)
		{
			throw Error(at, $"shifts on {type} are not supported (use int or long)");
		}
		int num = (wide ? 64 : 32);
		if (Read(count) is EX_IntConst eX_IntConst)
		{
			int num2 = eX_IntConst.Value & (num - 1);
			if (num2 == 0)
			{
				return value;
			}
			if (left)
			{
				return Op(BinaryOperatorKind.Multiply, value, K(1L << num2));
			}
			Value a = Op(BinaryOperatorKind.And, value, K(~((1L << num2) - 1)));
			if (num2 >= num - 1)
			{
				return Op(BinaryOperatorKind.Divide, Op(BinaryOperatorKind.Divide, a, K(1L << num2 - 1)), K(2L));
			}
			return Op(BinaryOperatorKind.Divide, a, K(1L << num2));
		}
		LocalVar localVar = Temp(type);
		LocalVar localVar2 = Temp(UType.Int);
		Store(localVar, Read(value));
		Store(localVar2, Read(Binary(BinaryOperatorKind.And, count, Constant(num - 1, null, at, UType.Int), UType.Int, at)));
		Label label = s.NewLabel("shift");
		Label label2 = s.NewLabel("endshift");
		s.Mark(label);
		s.JumpIfNot(CallMath("UKismetMathLibrary", "Greater_IntInt", localVar2.Expr(s), ScriptBuilder.Int(0)), label2);
		Store(localVar, Read(left ? Op(BinaryOperatorKind.Multiply, localVar, K(2L)) : Op(BinaryOperatorKind.Divide, Op(BinaryOperatorKind.And, localVar, K(-2L)), K(2L))));
		Store(localVar2, Read(Binary(BinaryOperatorKind.Subtract, localVar2, Constant(1, null, at, UType.Int), UType.Int, at)));
		s.Jump(label);
		s.Mark(label2);
		return localVar;
		Value K(long v)
		{
			if (!wide)
			{
				return Constant((int)v, null, at, type);
			}
			return Constant(v, null, at, type);
		}
		Value Op(BinaryOperatorKind k, Value left2, Value b)
		{
			return Binary(k, left2, b, type, at);
		}
	}

	private Value Step(Value v, bool increment, IOperation at)
	{
		return Binary(increment ? BinaryOperatorKind.Add : BinaryOperatorKind.Subtract, v, Constant(1, null, at, (v.Type == UType.Float) ? UType.Float : ((v.Type == UType.Double) ? UType.Double : ((v.Type == UType.Int64) ? UType.Int64 : ((v.Type == UType.Byte) ? UType.Byte : UType.Int)))), v.Type, at);
	}

	private Value Unary(IUnaryOperation u)
	{
		Value value = Lower(u.Operand);
		return u.OperatorKind switch
		{
			UnaryOperatorKind.Not => new RValue(CallMath("UKismetMathLibrary", "Not_PreBool", Read(value)), UType.Bool), 
			UnaryOperatorKind.Plus => value, 
			UnaryOperatorKind.Minus => Binary(BinaryOperatorKind.Subtract, Constant(0, null, u, value.Type), value, value.Type, u), 
			UnaryOperatorKind.BitwiseNegation => new RValue(CallMath("UKismetMathLibrary", (value.Type == UType.Int64) ? "Not_Int64" : "Not_Int", Read(value)), value.Type), 
			_ => throw Error(u, $"operator {u.OperatorKind} is not supported"), 
		};
	}

	private Value Convert(IConversionOperation c)
	{
		if (c.OperatorMethod != null)
		{
			string text = Symbols.IntrinsicId(c.OperatorMethod);
			if (text != null)
			{
				return Intrinsic(text, c, null, new IOperation[1] { c.Operand });
			}
		}
		UType uType = sym.Map(c.Type);
		Value value = Lower(c.Operand);
		if (uType == null)
		{
			ITypeSymbol? type = c.Type;
			if (type == null || type.SpecialType != SpecialType.System_Object)
			{
				ITypeSymbol? type2 = c.Type;
				if (type2 == null || type2.TypeKind != TypeKind.Interface || value.Type is UType.Object)
				{
					return value;
				}
			}
			throw Error(c, "boxing/object conversions are not supported");
		}
		return new RValue(CoerceExpr(value, uType, c, !c.IsImplicit), uType);
	}

	private KismetExpression Coerce(Value v, UType to, IOperation at)
	{
		return CoerceExpr(v, to, at, explicitCast: false);
	}

	private KismetExpression CoerceExpr(Value v, UType to, IOperation at, bool explicitCast)
	{
		UType type = v.Type;
		KismetExpression kismetExpression = Read(v);
		if (type.Equals(to))
		{
			return kismetExpression;
		}
		if (kismetExpression is EX_IntConst eX_IntConst && to is UType.Prim)
		{
			return Read(Constant(eX_IntConst.Value, null, at, to));
		}
		if (kismetExpression is EX_DoubleConst eX_DoubleConst && to is UType.Prim)
		{
			return Read(Constant(eX_DoubleConst.Value, null, at, to));
		}
		if (kismetExpression is EX_FloatConst eX_FloatConst && to is UType.Prim)
		{
			return Read(Constant(eX_FloatConst.Value, null, at, to));
		}
		EX_ByteConst eX_ByteConst = kismetExpression as EX_ByteConst;
		bool flag = eX_ByteConst != null;
		if (flag)
		{
			bool flag2 = ((to is UType.Prim || to is UType.Enum) ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			return Read(Constant(eX_ByteConst.Value, null, at, to));
		}
		if (kismetExpression is EX_Int64Const eX_Int64Const && to is UType.Prim)
		{
			return Read(Constant(eX_Int64Const.Value, null, at, to));
		}
		UType uType = type;
		if (uType is UType.Prim prim)
		{
			switch (prim.Kind)
			{
			case "IntProperty":
				break;
			case "ByteProperty":
				goto IL_031a;
			case "Int64Property":
				goto IL_0368;
			case "UInt64Property":
				if (to is UType.Prim prim3)
				{
					string kind = prim3.Kind;
					if (kind == "Int64Property")
					{
						return kismetExpression;
					}
				}
				goto IL_0803;
			case "FloatProperty":
				goto IL_03e8;
			case "DoubleProperty":
				goto IL_0428;
			case "BoolProperty":
				if (to is UType.Prim prim2)
				{
					string kind = prim2.Kind;
					if (kind == "IntProperty")
					{
						return call("Conv_BoolToInt", kismetExpression);
					}
				}
				goto IL_0803;
			case "StrProperty":
				goto IL_04a8;
			default:
				goto IL_0803;
			}
			if (to is UType.Prim prim4)
			{
				switch (prim4.Kind)
				{
				case "DoubleProperty":
					return call("Conv_IntToDouble", kismetExpression);
				case "FloatProperty":
					return call("Conv_DoubleToFloat", call("Conv_IntToDouble", kismetExpression));
				case "Int64Property":
					return call("Conv_IntToInt64", kismetExpression);
				case "ByteProperty":
					return call("Conv_IntToByte", kismetExpression);
				case "BoolProperty":
					return call("Conv_IntToBool", kismetExpression);
				}
			}
			else if (to is UType.Enum)
			{
				return call("Conv_IntToByte", kismetExpression);
			}
		}
		else if (uType is UType.Enum)
		{
			if (to is UType.Prim prim5)
			{
				switch (prim5.Kind)
				{
				case "IntProperty":
					break;
				case "DoubleProperty":
					goto IL_06bb;
				case "ByteProperty":
					goto IL_06c8;
				default:
					goto IL_0803;
				}
				goto IL_06ae;
			}
		}
		else if (uType is UType.Object obj)
		{
			if (to is UType.Object obj2)
			{
				if (obj.ClassPath == obj2.ClassPath || (!explicitCast && IsSubclass(obj.ClassPath, obj2.ClassPath)) || kismetExpression is EX_NoObject)
				{
					return kismetExpression;
				}
				return CastTo(obj2.ClassPath, kismetExpression);
			}
			if (to is UType.Class)
			{
				goto IL_07e2;
			}
			if (to is UType.Interface)
			{
				goto IL_07e6;
			}
			if (to is UType.WeakObject)
			{
				goto IL_07e8;
			}
		}
		else if (uType is UType.Class)
		{
			if (to is UType.Class)
			{
				return kismetExpression;
			}
			if (to is UType.Object)
			{
				goto IL_07e2;
			}
		}
		else if (uType is UType.SoftObject)
		{
			if (to is UType.SoftObject)
			{
				goto IL_07e4;
			}
		}
		else if (uType is UType.SoftClass)
		{
			if (to is UType.SoftClass)
			{
				goto IL_07e4;
			}
		}
		else if (uType is UType.Interface)
		{
			if (to is UType.Object)
			{
				goto IL_07e6;
			}
		}
		else if (uType is UType.WeakObject)
		{
			if (to is UType.Object)
			{
				goto IL_07e8;
			}
		}
		else if (uType is UType.Struct obj3)
		{
			if (to is UType.Struct obj4 && obj3.StructPath == obj4.StructPath)
			{
				return kismetExpression;
			}
		}
		else if (uType is UType.Delegate && to is UType.Delegate)
		{
			return kismetExpression;
		}
		goto IL_0803;
		IL_07e2:
		return kismetExpression;
		IL_04a8:
		if (to is UType.Prim prim6)
		{
			string kind = prim6.Kind;
			if (kind == "NameProperty")
			{
				return CallMath("UKismetStringLibrary", "Conv_StringToName", kismetExpression);
			}
			if (kind == "TextProperty")
			{
				return CallMath("UKismetTextLibrary", "Conv_StringToText", kismetExpression);
			}
		}
		goto IL_0803;
		IL_07e6:
		return kismetExpression;
		IL_07e4:
		return kismetExpression;
		IL_06c8:
		return kismetExpression;
		IL_0428:
		if (to is UType.Prim prim7)
		{
			switch (prim7.Kind)
			{
			case "FloatProperty":
				return call("Conv_DoubleToFloat", kismetExpression);
			case "IntProperty":
				return call("FTrunc", kismetExpression);
			case "Int64Property":
				return call("FTrunc64", kismetExpression);
			}
		}
		goto IL_0803;
		IL_031a:
		if (to is UType.Prim prim8)
		{
			string kind = prim8.Kind;
			if (kind == "IntProperty")
			{
				goto IL_06ae;
			}
			if (kind == "DoubleProperty")
			{
				goto IL_06bb;
			}
		}
		else if (to is UType.Enum)
		{
			goto IL_06c8;
		}
		goto IL_0803;
		IL_0803:
		if (type is UType.Object && to is UType.Object)
		{
			return kismetExpression;
		}
		throw Error(at, $"cannot convert {type} to {to}");
		IL_06ae:
		return call("Conv_ByteToInt", kismetExpression);
		IL_07e8:
		return kismetExpression;
		IL_0368:
		if (to is UType.Prim prim9)
		{
			switch (prim9.Kind)
			{
			case "IntProperty":
				return call("Conv_Int64ToInt", kismetExpression);
			case "UInt64Property":
				return kismetExpression;
			case "DoubleProperty":
				return call("Conv_Int64ToDouble", kismetExpression);
			}
		}
		goto IL_0803;
		IL_03e8:
		if (to is UType.Prim prim10)
		{
			string kind = prim10.Kind;
			if (kind == "DoubleProperty")
			{
				return call("Conv_FloatToDouble", kismetExpression);
			}
			if (kind == "IntProperty")
			{
				return call("FTrunc", call("Conv_FloatToDouble", kismetExpression));
			}
		}
		goto IL_0803;
		IL_06bb:
		return call("Conv_ByteToDouble", kismetExpression);
		KismetExpression call(string fnName, KismetExpression a)
		{
			return CallMath("UKismetMathLibrary", fnName, a);
		}
	}

	private bool IsSubclass(string cls, string ancestor)
	{
		return this.cls.IsSubclass(cls, ancestor);
	}

	private FPackageIndex ClassImport(ITypeSymbol t, IOperation at)
	{
		return pkg.ImportClass(sym.ClassPath(t) ?? throw Error(at, $"'{t}' is not an Unreal class"));
	}

	/// <summary>
	/// Whether obj is of the class (and not null). For an interface this asks DoesImplementInterface instead of
	/// EX_DynamicCast: cast to an interface, the VM writes an FScriptInterface (16 bytes) where the compiler keeps an
	/// object (8 bytes), overwriting what follows. With an object that implements the interface that is its own address,
	/// and it ended up corrupting the vtable pointer of mods' actors implementing IModSettings (crashing on their next
	/// timer or delegate call).
	/// </summary>
	private KismetExpression IsA(string classPath, KismetExpression obj)
	{
		FPackageIndex index = pkg.ImportClass(classPath);
		if (sym.IsInterface(classPath))
		{
			return CallMath("UKismetSystemLibrary", "DoesImplementInterface", obj, s.Object(index));
		}
		return CallMath("UKismetSystemLibrary", "IsValid", new EX_DynamicCast
		{
			ClassPtr = s.Ref(index),
			Target = obj
		});
	}

	/// <summary>obj cast to the class, or null; for an interface without EX_DynamicCast (see <see cref="IsA"/>).</summary>
	private KismetExpression CastTo(string classPath, KismetExpression obj)
	{
		FPackageIndex index = pkg.ImportClass(classPath);
		if (!sym.IsInterface(classPath))
		{
			return new EX_DynamicCast
			{
				ClassPtr = s.Ref(index),
				Target = obj
			};
		}
		LocalVar localVar = Temp(new UType.Object(classPath));
		Store(localVar, obj);
		return CallMath("UKismetMathLibrary", "SelectObject", localVar.Expr(s), ScriptBuilder.NoObject(), CallMath("UKismetSystemLibrary", "DoesImplementInterface", localVar.Expr(s), s.Object(index)));
	}

	private string TypeClassPath(ITypeSymbol t, IOperation at)
	{
		sym.Map(t);
		return sym.ClassPath(t) ?? throw Error(at, $"'{t}' is not an Unreal class");
	}

	private Value? ConditionalAccess(IConditionalAccessOperation ca, bool wantValue)
	{
		Var var = AsVar(Lower(ca.Operation));
		if (var.Type is UType.MulticastDelegate)
		{
			conditionalInstances.Push(var);
			try
			{
				Effect(ca.WhenNotNull);
			}
			finally
			{
				conditionalInstances.Pop();
			}
			return null;
		}
		UType uType = ((wantValue && ca.Type != null && ca.Type.SpecialType != SpecialType.System_Void) ? sym.Map(ca.Type) : null);
		if (wantValue && uType == null && ca.Type != null && ca.Type.SpecialType != SpecialType.System_Void)
		{
			throw Error(ca, $"'{ca.Type}' has no Unreal equivalent; check the object for null first instead of using ?. here");
		}
		LocalVar localVar = ((uType != null) ? Temp(uType) : null);
		if (localVar != null)
		{
			Store(localVar, Read(Default(uType, ca)));
		}
		Label label = s.NewLabel("nullcheck");
		s.JumpIfNot(CallMath("UKismetSystemLibrary", "IsValid", var.Expr(s)), label);
		conditionalInstances.Push(var);
		try
		{
			if (localVar != null)
			{
				Assign(localVar, ca.WhenNotNull);
			}
			else
			{
				Effect(ca.WhenNotNull);
			}
		}
		finally
		{
			conditionalInstances.Pop();
		}
		s.Mark(label);
		return localVar;
	}

	private Value IsPattern(IIsPatternOperation ip)
	{
		return Pattern(Lower(ip.Value), ip.Pattern, ip);
	}

	private Value Pattern(Value value, IPatternOperation pattern, IOperation ip)
	{
		if (!(pattern is IDeclarationPatternOperation { MatchedType: not null } declarationPatternOperation))
		{
			if (!(pattern is ITypePatternOperation typePatternOperation))
			{
				INegatedPatternOperation negatedPatternOperation;
				if (pattern is IConstantPatternOperation constantPatternOperation)
				{
					IOperation value2 = constantPatternOperation.Value;
					if (value2 != null)
					{
						Optional<object> constantValue = value2.ConstantValue;
						if (constantValue.HasValue && constantValue.Value == null)
						{
							return IsNull(value, ip);
						}
					}
					negatedPatternOperation = pattern as INegatedPatternOperation;
					if (negatedPatternOperation == null)
					{
						return Compare(BinaryOperatorKind.Equals, value, Lower(constantPatternOperation.Value), ip);
					}
				}
				else
				{
					negatedPatternOperation = pattern as INegatedPatternOperation;
					if (negatedPatternOperation == null)
					{
						throw Error(ip, "unsupported pattern");
					}
				}
				return new RValue(CallMath("UKismetMathLibrary", "Not_PreBool", Read(Pattern(value, negatedPatternOperation.Pattern, ip))), UType.Bool);
			}
			return new RValue(IsA(TypeClassPath(typePatternOperation.MatchedType, ip), Read(value)), UType.Bool);
		}
		KismetExpression value3 = CastTo(TypeClassPath(declarationPatternOperation.MatchedType, ip), Read(value));
		Var var = ((declarationPatternOperation.DeclaredSymbol is ILocalSymbol local) ? DeclareLocal(local, ip) : Temp(new UType.Object(sym.ClassPath(declarationPatternOperation.MatchedType))));
		Store(var, value3);
		return new RValue(CallMath("UKismetSystemLibrary", "IsValid", var.Expr(s)), UType.Bool);
	}
}
