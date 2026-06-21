using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

namespace Sprint5CombatUiHarness.Tests;

/// <summary>
/// Reflection-based test runner. Subclass it, decorate methods with [GodotTest],
/// attach the subclass as the root Node of a test scene, and Godot launches the
/// suite via --main-scene. Runner prints PASS/FAIL per test and quits with
/// exit code = failure count.
///
/// Engine.TimeScale is reset to 1.0 between tests to prevent leaks.
/// </summary>
public abstract partial class GodotTestRunner : Node
{
    public override async void _Ready()
    {
        int totalRun = 0;
        int totalFailed = 0;
        var results = new List<(string Id, bool Pass, string? Error)>();

        GD.Print("=== cu-006 Godot 4.7-stable integration test runner ===");
        GD.Print("Engine version: ", Engine.GetVersionInfo());
        GD.Print("Suite: ", GetType().Name);

        var methods = GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<GodotTestAttribute>() is not null)
            .OrderBy(m => m.Name)
            .ToArray();

        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<GodotTestAttribute>()!;
            totalRun++;
            Engine.TimeScale = 1.0d;

            try
            {
                object? ret = method.Invoke(this, null);
                if (ret is Task task)
                {
                    await task;
                }
                results.Add((attr.Id, true, null));
                GD.Print($"  PASS  {attr.Id}");
            }
            catch (Exception ex)
            {
                var inner = ex is TargetInvocationException tie && tie.InnerException is not null
                    ? tie.InnerException
                    : ex;
                totalFailed++;
                results.Add((attr.Id, false, inner.Message));
                GD.PrintErr($"  FAIL  {attr.Id}: {inner.Message}");
                GD.PrintErr(inner.StackTrace ?? "<no stack trace>");
            }
            finally
            {
                Engine.TimeScale = 1.0d;
            }
        }

        GD.Print("=== SUMMARY ===");
        foreach (var r in results)
        {
            GD.Print($"  {(r.Pass ? "PASS" : "FAIL")} {r.Id}");
        }
        GD.Print($"=== TOTAL: {totalRun} run, {totalRun - totalFailed} pass, {totalFailed} fail ===");

        int code = Math.Min(totalFailed, 125);
        GetTree().Quit(code);
    }
}
