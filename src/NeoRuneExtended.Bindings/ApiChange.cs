using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoRuneExtended.Bindings;

public sealed record ApiChange(string Subject, string Text)
{
	public bool Affects(IReadOnlyCollection<string> uses)
	{
		if (!uses.Contains(Subject))
		{
			return uses.Any((string u) => u.StartsWith(Subject + ":", StringComparison.Ordinal) || u.StartsWith(Subject + ".", StringComparison.Ordinal));
		}
		return true;
	}
}
