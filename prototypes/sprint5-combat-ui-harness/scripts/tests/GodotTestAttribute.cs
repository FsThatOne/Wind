using System;

namespace Sprint5CombatUiHarness.Tests;

/// <summary>
/// Marks a test method on a Node-derived class. The cu-006 integration runner discovers
/// these via reflection. Method must be public, parameterless, and return void or Task.
/// Name argument supplies the human-readable id printed in PASS/FAIL summary.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class GodotTestAttribute : Attribute
{
    public string Id { get; }

    public GodotTestAttribute(string id)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
    }
}
