using Microsoft.CodeAnalysis;

namespace NeoRuneExtended.Compiler;

public sealed record ModDiagnostic(string Severity, string Message, string? File, int Line, int Column)
{
	public override string ToString()
	{
		if (File == null)
		{
			return Severity + " NR0001: " + Message;
		}
		return $"{File}({Line},{Column}): {Severity} NR0001: {Message}";
	}

	public static ModDiagnostic From(string severity, string message, Location? location)
	{
		if (location == null || !location.IsInSource)
		{
			return new ModDiagnostic(severity, message, null, 0, 0);
		}
		FileLinePositionSpan lineSpan = location.GetLineSpan();
		return new ModDiagnostic(severity, message, lineSpan.Path, lineSpan.StartLinePosition.Line + 1, lineSpan.StartLinePosition.Character + 1);
	}
}
