using System;
using System.Collections;
using System.Collections.Generic;

namespace NeoRune;

/// <summary>Checks for <see cref="T:NeoRune.ModTestsAttribute" /> tests: each throws <see cref="T:NeoRune.ModAssertException" /> when it fails.</summary>
public static class ModAssert
{
	public static void True(bool condition, string message = "expected true")
	{
		if (!condition)
		{
			throw new ModAssertException(message);
		}
	}

	public static void False(bool condition, string message = "expected false")
	{
		True(!condition, message);
	}

	public static void Equal<T>(T expected, T actual, string? message = null)
	{
		if (EqualityComparer<T>.Default.Equals(expected, actual) || (expected is IEnumerable a && actual is IEnumerable b && !(expected is string) && SameItems(a, b)))
		{
			return;
		}
		throw new ModAssertException(((message != null) ? (message + ": ") : "") + "expected " + Show(expected) + ", got " + Show(actual));
	}

	public static void NotEqual<T>(T unexpected, T actual, string? message = null)
	{
		if (EqualityComparer<T>.Default.Equals(unexpected, actual))
		{
			throw new ModAssertException(((message != null) ? (message + ": ") : "") + "expected anything but " + Show(unexpected));
		}
	}

	/// <summary>Two numbers within <paramref name="tolerance" /> of each other.</summary>
	public static void Near(double expected, double actual, double tolerance = 1E-06)
	{
		if (Math.Abs(expected - actual) > tolerance)
		{
			throw new ModAssertException($"expected {expected} (±{tolerance}), got {actual}");
		}
	}

	public static void Fail(string message)
	{
		throw new ModAssertException(message);
	}

	private static bool SameItems(IEnumerable a, IEnumerable b)
	{
		IEnumerator enumerator = a.GetEnumerator();
		IEnumerator enumerator2 = b.GetEnumerator();
		do
		{
			bool flag = enumerator.MoveNext();
			bool flag2 = enumerator2.MoveNext();
			if (flag != flag2)
			{
				return false;
			}
			if (!flag)
			{
				return true;
			}
		}
		while (object.Equals(enumerator.Current, enumerator2.Current));
		return false;
	}

	private static string Show(object? value)
	{
		if (value != null)
		{
			if (!(value is string text))
			{
				if (value is IEnumerable items)
				{
					return "[" + string.Join(", ", Items(items)) + "]";
				}
				return value.ToString() ?? "";
			}
			return "\"" + text + "\"";
		}
		return "null";
	}

	private static IEnumerable<string> Items(IEnumerable items)
	{
		foreach (object? item in items)
		{
			yield return Show(item);
		}
	}
}
