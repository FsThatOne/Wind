using System;

namespace Sprint5CombatUiHarness.Tests;

/// <summary>
/// Lightweight assertion helpers used by [GodotTest] methods.
/// Throws TestAssertException on failure; runner catches and reports FAIL.
/// </summary>
public static class TestAssert
{
    public static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new TestAssertException(message);
        }
    }

    public static void False(bool condition, string message)
    {
        if (condition)
        {
            throw new TestAssertException(message);
        }
    }

    public static void Equal<T>(T expected, T actual, string message) where T : IEquatable<T>
    {
        if (!actual.Equals(expected))
        {
            throw new TestAssertException($"{message} — expected={expected} actual={actual}");
        }
    }

    public static void ApproxEqual(double expected, double actual, double tolerance, string message)
    {
        if (Math.Abs(actual - expected) > tolerance)
        {
            throw new TestAssertException($"{message} — expected={expected}±{tolerance} actual={actual}");
        }
    }

    public static void NotNull(object? value, string message)
    {
        if (value is null)
        {
            throw new TestAssertException(message);
        }
    }
}

public sealed class TestAssertException : Exception
{
    public TestAssertException(string message) : base(message) { }
}
