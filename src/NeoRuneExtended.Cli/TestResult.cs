namespace NeoRuneExtended.Cli;

internal sealed record TestResult(string Name, bool Passed, bool NeedsGame, string Message);
