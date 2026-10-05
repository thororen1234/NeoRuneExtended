namespace NeoRuneExtended.Compiler;

internal static class MethodPlanExtensions
{
	public static MethodPlan CopySignature(this MethodPlan to, MethodPlan from)
	{
		to.Params.AddRange(from.Params);
		to.ReturnName = from.ReturnName;
		to.ReturnType = from.ReturnType;
		return to;
	}
}
