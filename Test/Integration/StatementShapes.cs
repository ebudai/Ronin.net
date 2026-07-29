// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar;

namespace Integration;

/// <summary>
///     Every arrangement of statements in a block, enumerated rather than
///     chosen.
/// </summary>
///
/// <remarks>
///     <para>
///     «function f { if x { return 1; } return 2; }» did not compile, and 582
///     tests at 100% line and branch coverage did not notice. That is not a gap
///     in the tests — it is a property of the metric. Coverage measures which
///     lines ran, not which input SHAPES were formed, and a grammar's failure
///     modes live in combinations of constructs. Every block in every test
///     happened to be single-statement or block-final: a habit, invisible in a
///     coverage report, that no amount of line coverage would have revealed.
///     </para>
///     <para>
///     The remedy for a grammar is generative. This enumerates every sequence of
///     one to three elements drawn from a simple statement, a braced statement,
///     and a braced statement containing one — which is the smallest generator
///     that would have caught it on its first run.
///     </para>
/// </remarks>
[Trait(nameof(Parser), null)]
public class StatementShapes
{
    /// <summary>The shapes a block element can take.</summary>
    private static readonly (string Name, string Source)[] Elements =
    [
        ("simple", "return 1;"),
        ("braced", "if ready { return 1; }"),
        ("nested", "if ready { while going { return 1; } }"),
        ("loop", "for each bank in banks { return bank; }"),
    ];

    public static TheoryData<int> Lengths => [1, 2, 3];

    [Theory(DisplayName = "every sequence of block elements parses")]
    [MemberData(nameof(Lengths))]
    public void EverySequenceOfBlockElementsParses(int length)
    {
        var failures = new List<string>();

        foreach (var sequence in Sequences(length))
        {
            var body = string.Join(" ", sequence.Select(element => element.Source));
            var source = $"function f {{ {body} }}\n";

            Lexer lexer = new(source);
            Parser parser = new(lexer.Lex());

            var module = parser.Parse();

            if (module is Module.UnexpectedInputError || parser.IsNotFinished)
            {
                failures.Add(string.Join(" + ", sequence.Select(element => element.Name)));
                continue;
            }

            // and it is one function, not a function plus loose blocks — the bug
            // this catches produced BOTH, so "it parsed" is not the assertion
            if (module.Scopes[0].Statements is not [Function]) failures.Add($"{body} — not one function");
        }

        Assert.Empty(failures);
    }

    [Fact(DisplayName = "the same shapes at the top level of a file")]
    public void TheSameShapesAtTheTopLevelOfAFile()
    {
        // A module's statements are an aggregate too, with the same separator
        // rule, so the same combinations have to hold outside a function.
        foreach (var sequence in Sequences(2).Concat(Sequences(3)))
        {
            var source = string.Join(" ", sequence.Select(element => element.Source)) + "\n";

            Lexer lexer = new(source);
            Parser parser = new(lexer.Lex());

            Assert.IsNotType<Module.UnexpectedInputError>(parser.Parse());
            Assert.False(parser.IsNotFinished, source);
        }
    }

    [Theory(DisplayName = "the elision is the statement aggregate's, and no other's")]
    [InlineData("var nested values = { { 1 } { 2 } };\n", "var nested values = { { 1 }, { 2 } };\n")]
    [InlineData("var lookup = { { 1 } = { 2 } { 3 } = { 4 } };\n", "var lookup = { { 1 } = { 2 }, { 3 } = { 4 } };\n")]
    [InlineData("var r = f ({ 1 } { 2 });\n", "var r = f ({ 1 }, { 2 });\n")]
    public void TheElisionIsTheStatementAggregatesAndNoOthers(string missing, string separated)
    {
        // The generator above cannot see this. It exercises the aggregate at its
        // statement instantiation, and the elision was written into the generic
        // one — so lists, lookups and input blocks quietly stopped needing their
        // commas, and every program in this file still passed.
        //
        // A braced value ends in «}» exactly as a braced statement does, which is
        // why the rule could not tell them apart by looking at the tokens. It has
        // to ask which aggregate it is.
        var head = "function f (a => Number, b => Number) { return a; }\n";

        Assert.Equal(FindingKind.Malformed,
                     Assert.Single(Compilation.Of(new SourceText(head + missing, "P.ron")).Findings).Kind);

        Assert.Empty(Compilation.Of(new SourceText(head + separated, "P.ron")).Findings);
    }

    [Fact(DisplayName = "a block is split before anything is resolved")]
    public void ABlockIsSplitBeforeAnythingIsResolved()
    {
        // Statement boundaries are structural. A block is cut on «;» and on «}»
        // by the parser, and the resolver is then handed one element and either
        // resolves it or fails — so how many statements a program has cannot
        // depend on what names are in scope.
        //
        // «return 1 return 2; return 3;» is the case that tests it, because the
        // first element is one the resolver refuses. It is still ONE element:
        // the split happened before anyone asked what it meant.
        Lexer lexer = new("function f { return 1 return 2; return 3; }\n");
        Parser parser = new(lexer.Lex());

        var function = Assert.IsType<Function>(parser.Parse().Scopes[0].Statements[0]);

        Assert.Equal(2, function.Definition.Statements.Count);
    }

    private static IEnumerable<(string Name, string Source)[]> Sequences(int length)
    {
        if (length is 0) return [[]];

        return Sequences(length - 1).SelectMany(_ => Elements, (rest, element) => ((string, string)[])[.. rest, element]);
    }
}
