// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using System.Reflection;

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
        var shadowed = Assert.IsType<Shadowed>(finding);

        Assert.Equal("x", shadowed.Name);
        Assert.Equal("in this scope", shadowed.Where);
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
        Assert.Equal("in an enclosing scope", Assert.IsType<Shadowed>(finding).Where);
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
        // right, and each carries declarations the walk has to reach. Well-formed
        // source, because a malformed file stops before declarations are built at
        // all — the scoping walk and the error walk are answering different
        // questions and have to be asked separately.
        foreach (var nested in (string[])
                 [
                     "var total => Number;\n{ var total => Number; }\n",
                     "var total => Number;\nif ready { var total => Number; }\n",
                     "var total => Number;\nwhile ready { var total => Number; }\n",
                 ])
        {
            var finding = Assert.Single(Of(nested));

            Assert.Equal(FindingKind.Shadowed, finding.Kind);
            Assert.Equal("in an enclosing scope", Assert.IsType<Shadowed>(finding).Where);
        }
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

    [Theory(DisplayName = "an error anywhere in the tree is found")]
    // in a declaration's own parameter block, reached only through the
    // identifier — these two killed the process, because declaration building
    // dereferenced a parameter that had no identifier to give
    [InlineData("function f (var +) {}\n")]
    [InlineData("var f (var +) => Number;\n")]
    // in a delegate's parameters
    [InlineData("var callback = (var +) => {};\n")]
    // in a lookup, and in an input block — an association whose value is missing
    [InlineData("var value = { key = };\n")]
    [InlineData("var value = (key = );\n")]
    public void AnErrorAnywhereInTheTreeIsFound(string source)
    {
        // The walk this replaces descended scope bodies and carried a comment
        // saying an error could only ever be in a statement position. A lookup
        // holds associations, a delegate holds parameters, and an identifier
        // holds parameter blocks — all of them can hold a recovery node, and
        // none of them is a statement position.
        Assert.Equal(FindingKind.Malformed, Single(source));
    }

    [Fact(DisplayName = "a pattern past its width is refused, not thrown")]
    public void APatternPastItsWidthIsRefusedNotThrown()
    {
        // The ceiling exists to stop a declaration being a way to exhaust the
        // stack, and it was enforced by a constructor throwing — so source wide
        // enough to reach it terminated compilation instead of being rejected.
        // A bound that refuses hostile input by killing the compiler is not a
        // bound.
        string wide(int words) => "function " + string.Concat(Enumerable.Repeat("word ", words)) + "(x => Number) {}\n";

        Assert.Empty(Of(wide(Ronin.Compiler.Pattern.MaxSegments - 1)));

        var finding = Assert.Single(Of(wide(Ronin.Compiler.Pattern.MaxSegments)));
        var refused = Assert.IsType<PatternTooWide>(finding);

        Assert.Equal(Ronin.Compiler.Pattern.MaxSegments + 1, refused.Width);
        Assert.Equal(Ronin.Compiler.Pattern.MaxSegments, refused.Most);
    }

    [Fact(DisplayName = "a wide plain name is not a wide pattern")]
    public void AWidePlainNameIsNotAWidePattern()
    {
        // Having holes is what makes something a pattern, and it is the only
        // thing that does. Deciding by width alone told someone with a 129-word
        // NAME that "a pattern may have at most 128" — true, and not about them,
        // and quoting a limit on a matcher their declaration never enters.
        var name = string.Join(' ', Enumerable.Repeat("word", Ronin.Compiler.Pattern.MaxSegments + 1));

        Assert.Empty(Of($"var {name} => Number;\n"));

        // while the same width WITH a hole in it is still refused
        var pattern = string.Concat(Enumerable.Repeat("word ", Ronin.Compiler.Pattern.MaxSegments));

        Assert.IsType<PatternTooWide>(Assert.Single(Of($"function {pattern}(x => Number) {{}}\n")));
    }

    [Fact(DisplayName = "compiling two files at once does not corrupt the walk")]
    public void CompilingTwoFilesAtOnceDoesNotCorruptTheWalk()
    {
        // The reflected member cache is process-wide and a compilation is not.
        // An unsynchronised check-then-assign corrupted it the first time two
        // files were compiled together — five runs out of five — and today's CLI
        // escapes it only by looping one file at a time, which is a property of
        // that loop and not of this type.
        var sources = Enumerable.Range(0, 64)
                                .Select(each => $"var name{each} => Number;\n" +
                                                $"function f{each} (x => Number) {{ return x; }}\n")
                                .ToArray();

        System.Collections.Concurrent.ConcurrentBag<Exception> failures = [];

        Parallel.For(0, sources.Length, each =>
        {
            try { Compilation.Of(new SourceText(sources[each], $"f{each}.ron")); }
            catch (Exception corrupted) { failures.Add(corrupted); }
        });

        Assert.Empty(failures);
    }

    [Fact(DisplayName = "a synthesised type is not a syntax node, and does not throw")]
    public void ASynthesisedTypeIsNotASyntaxNodeAndDoesNotThrow()
    {
        // The error walk asks every value it reaches whether it is a syntax
        // node, and a named type has a namespace — which is true of every type
        // anyone WRITES and false of the ones the compiler synthesises. A
        // collection expression assigned to an «IReadOnlyList» produces a
        // read-only wrapper with no namespace at all, and the moment a grammar
        // node exposed one, the walk threw on every compilation.
        //
        // The member filter keeps those slots out of the walk now, so this is no
        // longer reached through source — which is exactly why it is asserted
        // here. The next grammar property returning «[.. something]» of a syntax
        // type would put the wrapper back in front of it.
        IReadOnlyList<int> synthesised = [1];

        Assert.Null(synthesised.GetType().Namespace);

        var method = typeof(Compilation).GetMethod("IsSyntax",
                                                   BindingFlags.NonPublic | BindingFlags.Static,
                                                   [typeof(Type)]);

        Assert.False((bool)method.Invoke(null, [synthesised.GetType()]));
        Assert.True((bool)method.Invoke(null, [typeof(Ronin.Grammar.Datum)]));
    }

    /// <summary>A named collection of syntax, which is not itself generic.</summary>
    private sealed class Nested : List<Ronin.Grammar.Statement>;

    [Theory(DisplayName = "only a slot that could hold a child is read")]
    // could: a syntax type, a collection of one, or a slot typed loosely enough
    [InlineData(typeof(Ronin.Grammar.Datum), true)]
    [InlineData(typeof(object), true)]
    [InlineData(typeof(IReadOnlyList<Ronin.Grammar.Statement>), true)]
    [InlineData(typeof(Ronin.Grammar.Statement[]), true)]
    // could: a named collection is not generic itself, and an untyped one says
    // nothing about its elements — «Children» can enumerate either
    [InlineData(typeof(Nested), true)]
    [InlineData(typeof(System.Collections.IEnumerable), true)]
    [InlineData(typeof(System.Collections.ArrayList), true)]
    // could not: the shape of a computed answer, not of a tree
    [InlineData(typeof(bool), false)]
    [InlineData(typeof(int), false)]
    [InlineData(typeof(string), false)]
    [InlineData(typeof(IReadOnlyList<string>), false)]
    [InlineData(typeof(string[]), false)]
    public void OnlyASlotThatCouldHoldAChildIsRead(Type slot, bool read)
    {
        // The walk reads every member BEFORE asking whether the answer is
        // syntax, so a computed property was being evaluated for its side
        // effects on the clock: «Identifier.Writable» renders the whole shape
        // and lexes it back, and a «bool» has never held a syntax node. Every
        // declaration paid that readback here and again in «TryPattern», and an
        // over-width one paid it in full before the guard that bounds it.
        //
        // Generous about what it admits and exact about what it rejects —
        // «object» is admitted because a slot typed that loosely could hold
        // anything, and skipping a real child would put this walk back where the
        // hand-written one was.
        var method = typeof(Compilation).GetMethod("Holds", BindingFlags.NonPublic | BindingFlags.Static,
                                                   [typeof(Type)]);

        Assert.Equal(read, (bool)method.Invoke(null, [slot]));
    }

    [Fact(DisplayName = "unrecognisable input is a problem, not a statement")]
    public void UnrecognisableInputIsAProblemNotAStatement()
    {
        // Recovery is not acceptance. Unknown held its tokens and implemented no
        // error contract, so the catch-all that keeps the parser moving was also
        // the one that made anything at all legal.
        Assert.Equal(FindingKind.Malformed, Single("+;\n"));
    }

    [Fact(DisplayName = "one mistake is one finding, brace and all")]
    public void OneMistakeIsOneFindingBraceAndAll()
    {
        // Recovery stopped at the first closer, so «function f => {}» consumed
        // its «{» and left the «}» for the unexpected-input path: a missing type
        // AND a stray brace, from one mistake, under a message promising the rest
        // of the statement had been skipped.
        Assert.Equal(FindingKind.Malformed, Single("function f => {}\n"));

        foreach (var dangling in (string[])["var x => = 1;\n", "var x => ;\n", "var x = ;\n", "type T = ;\n"])
        {
            Assert.Single(Of(dangling));
        }
    }

    [Fact(DisplayName = "input no statement accounts for is still reported")]
    public void InputNoStatementAccountsForIsStillReported()
    {
        // The one error that is not a statement and so is not reachable by any
        // walk over statements.
        Assert.Equal(FindingKind.Malformed, Single("var total => Number;\n}\n"));
    }
}
