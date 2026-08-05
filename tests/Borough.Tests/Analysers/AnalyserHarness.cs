using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Borough.Tests.Analysers;

/// <summary>
/// Compiles a probe and runs one analyser over it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every diagnostic in Borough.Analysers has a test that writes the violation and watches the
/// diagnostic fire.</b> <c>dev-environment.md</c> A5 already insists on this for the Godot guard —
/// <c>Core_does_not_reference_Godot</c> has been seen to fail when Godot is actually used from
/// <c>Borough.Core</c> — and the reasoning generalises without weakening: <em>a guard nobody has
/// watched fail is a guard nobody knows is wired up</em>. An analyser that silently stopped
/// registering would leave a green suite and an unenforced rule, which is the exact state slice 3
/// exists to leave behind.
/// </para>
/// <para>
/// <b>The negative cases carry as much weight as the positive ones</b> and are not padding. A lint
/// that fires on a legal construct gets suppressed at the site where it was right, so
/// <c>Silent</c> pins the boundary: a Dictionary built and looked up, a constant shift count, a
/// SortedDictionary walked, a documented cold-path exception.
/// </para>
/// </remarks>
internal static class AnalyserHarness
{
    /// <summary>Asserts that <paramref name="source"/> produces exactly the diagnostic named.</summary>
    internal static void Fires(
        string expectedId, DiagnosticAnalyzer analyser, string source, bool referenceCore = true)
    {
        ImmutableArray<Diagnostic> reported = Analyse(analyser, source, referenceCore);

        Assert.True(
            reported.Any(d => d.Id == expectedId),
            $"{expectedId} did not fire on a deliberate violation. Reported instead: " +
            (reported.IsEmpty ? "nothing" : string.Join("; ", reported.Select(d => d.Id))) +
            ". A guard nobody has watched fail is a guard nobody knows is wired up.");
    }

    /// <summary>Asserts that <paramref name="source"/> is left alone.</summary>
    internal static void Silent(DiagnosticAnalyzer analyser, string source, bool referenceCore = true)
    {
        ImmutableArray<Diagnostic> reported = Analyse(analyser, source, referenceCore);

        Assert.True(
            reported.IsEmpty,
            "a legal construct was reported: " +
            string.Join("; ", reported.Select(d => $"{d.Id} — {d.GetMessage()}")) +
            ". A lint that is sometimes wrong gets suppressed at the site where it was right.");
    }

    private static ImmutableArray<Diagnostic> Analyse(
        DiagnosticAnalyzer analyser, string source, bool referenceCore)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "AnalyserProbe",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references: referenceCore ? TestReferences.WithCore : TestReferences.PlatformOnly,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Without this, a typo in a probe makes the test vacuous: the analyser reports nothing
        // because there was nothing to analyse, and `Fires` fails for a reason that reads like a
        // broken analyser while `Silent` passes for no reason at all.
        var invalid = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        Assert.True(invalid.Count == 0,
            "the probe is not valid C#, so this test proves nothing: " +
            string.Join("; ", invalid.Select(d => d.GetMessage())));

        return compilation
            .WithAnalyzers(ImmutableArray.Create(analyser))
            .GetAnalyzerDiagnosticsAsync()
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>Wraps statements in a method, in a namespace with no exemption attached to it.</summary>
    internal static string InMethod(string statements) => $$"""
        using System;
        using System.Collections.Generic;
        using System.Linq;

        namespace Probe;

        internal static class Subject
        {
            internal static void Run()
            {
        {{statements}}
            }
        }
        """;
}
