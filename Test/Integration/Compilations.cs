// Copyright © 2026 Eric Budai

using Ronin.Compiler;

namespace Integration;

/// <summary>
///     Source in, findings out, by the path the executable actually takes.
/// </summary>
///
/// <remarks>
///     Every phase was tested and nothing tested the joins, so the compiler
///     accepted a file that each phase would separately have rejected: parse
///     errors were carried in the tree and never read out of it, and the
///     declaration rules were run on the outermost statements only. The nested
///     tests passed because they did the traversal themselves.
/// </remarks>
[Trait(nameof(Compilation), null)]
public class Compilations
{
    private static IReadOnlyList<Finding> Of(string source) => Compilation.Of(new SourceText(source, "Player.ron")).Findings;

    private static FindingKind Single(string source) => Assert.Single(Of(source)).Kind;

    [Fact(DisplayName = "a parse error below the top level is reported")]
    public void AParseErrorBelowTheTopLevelIsReported()
    {
        // It was in the tree the whole time. Program recognised exactly one error
        // type — the module's own — and never walked statements for the rest, so
        // «function ;» compiled cleanly and said "1 statement, 0 problems".
        Assert.Equal(FindingKind.Malformed, Single("function ;\n"));

        // and one inside a body, which needs the walk to descend
        Assert.Equal(FindingKind.Malformed, Single("function f { var + ; }\n"));
    }

    [Fact(DisplayName = "a duplicate declaration inside a body is reported")]
    public void ADuplicateDeclarationInsideABodyIsReported()
    {
        // The audit's own file. Declarations.Of was called on the module's outer
        // statements and never on a type, function or scope body, so this was
        // "1 statement, 1 name, 0 patterns" and exit zero.
        var finding = Assert.Single(Of("""
                                       type Box {
                                           var x => Number;
                                           var x => Number;
                                       }

                                       """));

        Assert.Equal(FindingKind.Shadowed, finding.Kind);
        Assert.Equal("x", finding["name"]);
        Assert.Equal("in this scope", finding["where"]);
    }

    [Fact(DisplayName = "a body sees the scope enclosing it")]
    public void ABodySeesTheScopeEnclosingIt()
    {
        // Inward yes: the inner «total» collides with the outer one, and the
        // message says which direction to look.
        var finding = Assert.Single(Of("""
                                       var total => Number;
                                       function recompute { var total => Number; }

                                       """));

        Assert.Equal(FindingKind.Shadowed, finding.Kind);
        Assert.Equal("in an enclosing scope", finding["where"]);
    }

    [Fact(DisplayName = "a sibling body is not visible")]
    public void ASiblingBodyIsNotVisible()
    {
        // Outward no. Two functions may each declare «count» without collision,
        // which is the whole point of a body being a scope — and would not be
        // true if the walk merged bodies into their parent instead of nesting
        // them.
        Assert.Empty(Of("""
                        function first { var count => Number; }
                        function second { var count => Number; }

                        """));
    }

    [Fact(DisplayName = "an outer conflict is reported once, not once per body")]
    public void AnOuterConflictIsReportedOnceNotOncePerBody()
    {
        // The rules run over the MERGED table, so a conflict between two outer
        // declarations is found again inside every scope nested below them. It is
        // one mistake and gets one finding.
        var finding = Assert.Single(Of("""
                                       var total => Number;
                                       var total => Number;
                                       function a { var x => Number; }
                                       function b { var y => Number; }

                                       """));

        Assert.Equal(FindingKind.Shadowed, finding.Kind);
    }

    [Fact(DisplayName = "a clean file has nothing to say")]
    public void ACleanFileHasNothingToSay()
    {
        var compilation = Compilation.Of(new SourceText("""
                                                        var base price => Number;
                                                        function compute total for (order => Number) { return order; }

                                                        """, "Player.ron"));

        Assert.Empty(compilation.Findings);

        // and the declarations are the outermost scope's, with «old base price»
        // injected beside the name that caused it
        Assert.Contains("base price", compilation.Declarations.Symbols.Names);
        Assert.Contains("old base price", compilation.Declarations.Symbols.Names);
        Assert.Single(compilation.Declarations.Symbols.Patterns);
    }

    [Fact(DisplayName = "a scope that is not a function or a type is walked too")]
    public void AScopeThatIsNotAFunctionOrATypeIsWalkedTo()
    {
        // A bare block, a conditional and a loop are all scopes in their own
        // right, and each carries statements the walk has to reach.
        Assert.Equal(FindingKind.Malformed, Single("{ function ; }\n"));
        Assert.Equal(FindingKind.Malformed, Single("if ready { function ; }\n"));
        Assert.Equal(FindingKind.Malformed, Single("while ready { function ; }\n"));
    }

    [Fact(DisplayName = "a declaration with no body is not a body with no declarations")]
    public void ADeclarationWithNoBodyIsNotABodyWithNoDeclarations()
    {
        // «type T;» has no member block at all, and an error node is a Function
        // or a Type too while carrying none of the parts a real one would — so
        // every part the walk descends through can legitimately be absent.
        Assert.Empty(Of("type Colour;\n"));

        // the error node whose Definition was never built
        Assert.Equal(FindingKind.Malformed, Single("function ;\n"));
    }

    [Fact(DisplayName = "input no statement accounts for is still reported")]
    public void InputNoStatementAccountsForIsStillReported()
    {
        // The one error that is not a statement and so is not reachable by any
        // walk over statements.
        Assert.Equal(FindingKind.Malformed, Single("var total => Number;\n}\n"));
    }
}
