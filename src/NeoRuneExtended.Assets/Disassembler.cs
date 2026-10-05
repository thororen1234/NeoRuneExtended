using System.IO;
using System.Linq;
using System.Text;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.FieldTypes;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.UnrealTypes;

namespace NeoRuneExtended.Assets;

public static class Disassembler
{
	public static string Disassemble(string uassetPath)
	{
		UAsset uAsset = new UAsset(uassetPath, EngineVersion.VER_UE5_6);
		StringBuilder stringBuilder = new StringBuilder();
		foreach (Export export in uAsset.Exports)
		{
			if (export is ClassExport classExport)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder3 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 2, stringBuilder2);
				handler.AppendLiteral("class ");
				handler.AppendFormatted(export.ObjectName);
				handler.AppendLiteral(" : ");
				handler.AppendFormatted(Name(uAsset, classExport.SuperStruct));
				stringBuilder3.AppendLine(ref handler);
				FProperty[] loadedProperties = classExport.LoadedProperties;
				foreach (FProperty fProperty in loadedProperties)
				{
					stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder4 = stringBuilder2;
					handler = new StringBuilder.AppendInterpolatedStringHandler(8, 2, stringBuilder2);
					handler.AppendLiteral("  var ");
					handler.AppendFormatted(fProperty.Name);
					handler.AppendLiteral(": ");
					handler.AppendFormatted(PropType(uAsset, fProperty));
					stringBuilder4.AppendLine(ref handler);
				}
			}
			if (export is FunctionExport functionExport)
			{
				stringBuilder.AppendLine();
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder5 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(12, 3, stringBuilder2);
				handler.AppendLiteral("function ");
				handler.AppendFormatted(functionExport.ObjectName);
				handler.AppendLiteral(" [");
				handler.AppendFormatted(functionExport.FunctionFlags);
				handler.AppendLiteral("]");
				handler.AppendFormatted((functionExport.SuperIndex.Index != 0) ? (" overrides " + Name(uAsset, functionExport.SuperIndex)) : "");
				stringBuilder5.AppendLine(ref handler);
				FProperty[] loadedProperties = functionExport.LoadedProperties;
				foreach (FProperty fProperty2 in loadedProperties)
				{
					stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder6 = stringBuilder2;
					handler = new StringBuilder.AppendInterpolatedStringHandler(5, 4, stringBuilder2);
					handler.AppendLiteral("  ");
					handler.AppendFormatted(fProperty2.PropertyFlags.HasFlag(EPropertyFlags.CPF_Parm) ? "param" : "local");
					handler.AppendLiteral(" ");
					handler.AppendFormatted(fProperty2.Name);
					handler.AppendLiteral(": ");
					handler.AppendFormatted(PropType(uAsset, fProperty2));
					handler.AppendFormatted(fProperty2.PropertyFlags.HasFlag(EPropertyFlags.CPF_ReturnParm) ? " (return)" : (fProperty2.PropertyFlags.HasFlag(EPropertyFlags.CPF_OutParm) ? " (out)" : ""));
					stringBuilder6.AppendLine(ref handler);
				}
				uint num = 0u;
				KismetExpression[] scriptBytecode = functionExport.ScriptBytecode;
				foreach (KismetExpression e in scriptBytecode)
				{
					stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder7 = stringBuilder2;
					handler = new StringBuilder.AppendInterpolatedStringHandler(4, 2, stringBuilder2);
					handler.AppendLiteral("  ");
					handler.AppendFormatted(num, 5);
					handler.AppendLiteral(": ");
					handler.AppendFormatted(Format(uAsset, e));
					stringBuilder7.AppendLine(ref handler);
					num += (uint)Size(uAsset, e);
				}
			}
		}
		return stringBuilder.ToString();
	}

	private static int Size(UAsset asset, KismetExpression e)
	{
		using MemoryStream stream = new MemoryStream();
		using AssetBinaryWriter writer = new AssetBinaryWriter(stream, asset);
		return ExpressionSerializer.WriteExpression(e, writer);
	}

	private static string Name(UAsset a, FPackageIndex? i)
	{
		if (i == null || i.Index == 0)
		{
			return "null";
		}
		if (!i.IsImport())
		{
			return i.ToExport(a).ObjectName.ToString();
		}
		return i.ToImport(a).ObjectName.ToString();
	}

	private static string PropType(UAsset a, FProperty p)
	{
		if (!(p is FArrayProperty fArrayProperty))
		{
			if (!(p is FClassProperty fClassProperty))
			{
				if (!(p is FObjectProperty fObjectProperty))
				{
					if (!(p is FStructProperty fStructProperty))
					{
						if (p is FMapProperty fMapProperty)
						{
							return $"Map<{PropType(a, fMapProperty.KeyProp)},{PropType(a, fMapProperty.ValueProp)}>";
						}
						return p.SerializedType.ToString().Replace("Property", "");
					}
					return "Struct<" + Name(a, fStructProperty.Struct) + ">";
				}
				return $"{p.SerializedType}<{Name(a, fObjectProperty.PropertyClass)}>";
			}
			return "Class<" + Name(a, fClassProperty.MetaClass) + ">";
		}
		return "Array<" + PropType(a, fArrayProperty.Inner) + ">";
	}

	private static string Ptr(KismetPropertyPointer? p)
	{
		FName[] array = p?.New?.Path;
		if (array == null || array.Length <= 0)
		{
			return "-";
		}
		return string.Join(".", array.Select((FName n) => n.ToString()));
	}

	private static string Args(UAsset a, KismetExpression[] ps)
	{
		return string.Join(", ", ps.Select((KismetExpression p) => Format(a, p)));
	}

	public static string Format(UAsset a, KismetExpression e)
	{
		if (!(e is EX_LocalVariable eX_LocalVariable))
		{
			if (!(e is EX_LocalOutVariable eX_LocalOutVariable))
			{
				if (!(e is EX_InstanceVariable eX_InstanceVariable))
				{
					if (!(e is EX_IntConst { Value: var value }))
					{
						if (!(e is EX_ByteConst { Value: var value2 }))
						{
							if (!(e is EX_FloatConst eX_FloatConst))
							{
								if (!(e is EX_DoubleConst eX_DoubleConst))
								{
									if (!(e is EX_Int64Const { Value: var value3 }))
									{
										if (!(e is EX_StringConst eX_StringConst))
										{
											if (!(e is EX_UnicodeStringConst eX_UnicodeStringConst))
											{
												if (!(e is EX_NameConst eX_NameConst))
												{
													if (!(e is EX_True))
													{
														if (!(e is EX_False))
														{
															if (!(e is EX_Self))
															{
																if (!(e is EX_NoObject))
																{
																	if (!(e is EX_Nothing))
																	{
																		if (!(e is EX_ObjectConst eX_ObjectConst))
																		{
																			if (!(e is EX_CallMath eX_CallMath))
																			{
																				if (!(e is EX_CallMulticastDelegate eX_CallMulticastDelegate))
																				{
																					if (!(e is EX_LocalFinalFunction eX_LocalFinalFunction))
																					{
																						if (!(e is EX_FinalFunction eX_FinalFunction))
																						{
																							if (!(e is EX_LocalVirtualFunction eX_LocalVirtualFunction))
																							{
																								if (!(e is EX_VirtualFunction eX_VirtualFunction))
																								{
																									if (!(e is EX_Context eX_Context))
																									{
																										if (!(e is EX_Let eX_Let))
																										{
																											if (!(e is EX_LetBase eX_LetBase))
																											{
																												if (!(e is EX_JumpIfNot eX_JumpIfNot))
																												{
																													if (!(e is EX_Jump eX_Jump))
																													{
																														if (!(e is EX_DynamicCast eX_DynamicCast))
																														{
																															if (!(e is EX_SetArray eX_SetArray))
																															{
																																if (!(e is EX_Return eX_Return))
																																{
																																	if (!(e is EX_EndOfScript))
																																	{
																																		if (!(e is EX_StructMemberContext eX_StructMemberContext))
																																		{
																																			if (!(e is EX_InstanceDelegate eX_InstanceDelegate))
																																			{
																																				if (!(e is EX_AddMulticastDelegate eX_AddMulticastDelegate))
																																				{
																																					if (e is EX_RemoveMulticastDelegate eX_RemoveMulticastDelegate)
																																					{
																																						return Format(a, eX_RemoveMulticastDelegate.Delegate) + " -= " + Format(a, eX_RemoveMulticastDelegate.DelegateToAdd);
																																					}
																																					if (e is EX_BindDelegate eX_BindDelegate)
																																					{
																																						return $"{Format(a, eX_BindDelegate.Delegate)} = bind {Format(a, eX_BindDelegate.ObjectTerm)}.{eX_BindDelegate.FunctionName}";
																																					}
																																					if (e is EX_ClearMulticastDelegate eX_ClearMulticastDelegate)
																																					{
																																						return "clear " + Format(a, eX_ClearMulticastDelegate.DelegateToClear);
																																					}
																																					return e.GetType().Name;
																																				}
																																				return Format(a, eX_AddMulticastDelegate.Delegate) + " += " + Format(a, eX_AddMulticastDelegate.DelegateToAdd);
																																			}
																																			return $"delegate {eX_InstanceDelegate.FunctionName}";
																																		}
																																		return Format(a, eX_StructMemberContext.StructExpression) + "." + Ptr(eX_StructMemberContext.StructMemberExpression);
																																	}
																																	return "end";
																																}
																																return "return " + Format(a, eX_Return.ReturnExpression);
																															}
																															return Format(a, eX_SetArray.AssigningProperty) + " = [" + Args(a, eX_SetArray.Elements) + "]";
																														}
																														return $"cast<{Name(a, eX_DynamicCast.ClassPtr)}>({Format(a, eX_DynamicCast.Target)})";
																													}
																													return $"goto {eX_Jump.CodeOffset}";
																												}
																												return $"if !({Format(a, eX_JumpIfNot.BooleanExpression)}) goto {eX_JumpIfNot.CodeOffset}";
																											}
																											return Format(a, eX_LetBase.VariableExpression) + " = " + Format(a, eX_LetBase.AssignmentExpression);
																										}
																										return Format(a, eX_Let.Variable) + " = " + Format(a, eX_Let.Expression);
																									}
																									return Format(a, eX_Context.ObjectExpression) + " -> " + Format(a, eX_Context.ContextExpression);
																								}
																								return $"virtual {eX_VirtualFunction.VirtualFunctionName}({Args(a, eX_VirtualFunction.Parameters)})";
																							}
																							return $"this.virtual {eX_LocalVirtualFunction.VirtualFunctionName}({Args(a, eX_LocalVirtualFunction.Parameters)})";
																						}
																						return Name(a, eX_FinalFunction.StackNode) + "(" + Args(a, eX_FinalFunction.Parameters) + ")";
																					}
																					return $"this.{Name(a, eX_LocalFinalFunction.StackNode)}({Args(a, eX_LocalFinalFunction.Parameters)})";
																				}
																				return $"call multicast {Format(a, eX_CallMulticastDelegate.Delegate)}({Args(a, eX_CallMulticastDelegate.Parameters)})";
																			}
																			return Name(a, eX_CallMath.StackNode) + "(" + Args(a, eX_CallMath.Parameters) + ")";
																		}
																		return "@" + Name(a, eX_ObjectConst.Value);
																	}
																	return "nothing";
																}
																return "null";
															}
															return "self";
														}
														return "false";
													}
													return "true";
												}
												return $"name\"{eX_NameConst.Value}\"";
											}
											return "\"" + eX_UnicodeStringConst.Value + "\"";
										}
										return "\"" + eX_StringConst.Value + "\"";
									}
									return value3 + "L";
								}
								return eX_DoubleConst.Value.ToString("R");
							}
							return eX_FloatConst.Value + "f";
						}
						return value2 + "b";
					}
					return value.ToString();
				}
				return "self." + Ptr(eX_InstanceVariable.Variable);
			}
			return "out $" + Ptr(eX_LocalOutVariable.Variable);
		}
		return "$" + Ptr(eX_LocalVariable.Variable);
	}
}
