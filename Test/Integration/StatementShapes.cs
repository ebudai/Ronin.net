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

    [Theory(DisplayName = "a multi-word keyword is one word from source to resolution")]
    [InlineData("part of")]
    [InlineData("for each")]
    public void AMultiWordKeywordIsOneWordFromSourceToResolution(string keyword)
    {
        // The whole path, because each layer was canonical on its own terms and
        // they disagreed. The resolver folded the spacing; declarations kept the
        // source slice. So «var ready part of world» and «var ready part  of
        // world» were TWO names to the symbol table and one to the resolver: a
        // duplicate nothing reported, and a second copy nothing could reach.
        foreach (var spacing in (string[])[" ", "  ", "\t", "\n"])
        {
            var spaced = keyword.Replace(" ", spacing);

            // one name, whatever the spacing, and the same one every time
            var declared = Compilation.Of(new SourceText($"var ready {spaced} world => Number;\n", "P.ron"));

            Assert.Empty(declared.Findings);
            Assert.Contains($"ready {keyword} world", declared.Declarations.Symbols.Names);

            // and a pattern anchored on it matches what the lexer produces,
            // which is what «Split(' ')» could not do: it made four segments
            // where a call lexes to three, so the pattern was declared, printed
            // correctly, and could never match anything
            var pattern = Compilation.Of(new SourceText($"function compute {spaced} (x => Number) {{ return x; }}\n", "P.ron"));

            Assert.Empty(pattern.Findings);
            Assert.Equal($"compute {keyword} (_)", Assert.Single(pattern.Declarations.Symbols.Patterns).ToString());

            Assert.Equal("Resolved",
                         new Resolver(pattern.Declarations.Symbols).Resolve($"compute {spaced} 1").Kind.ToString());
        }
    }

    [Theory(DisplayName = "a name is its words, however they were spaced")]
    [InlineData("part of")]
    [InlineData("for each")]
    public void ANameIsItsWordsHoweverTheyWereSpaced(string keyword)
    {
        // Equality and hashing were over raw token text, so two names with the
        // same words, the same rendering and the same symbol-table key compared
        // unequal and hashed apart — an identity that disagreed with every other
        // layer's.
        var names = new[] { " ", "  ", "\t", "\n" }
                    .Select(spacing => Named($"var ready {keyword.Replace(" ", spacing)} world => Number;\n"))
                    .ToArray();

        Assert.All(names, name => Assert.Equal(names[0], name));
        Assert.All(names, name => Assert.Equal(names[0].GetHashCode(), name.GetHashCode()));

        Assert.NotEqual(names[0], Named($"var ready {keyword} planet => Number;\n"));
    }

    private static Ronin.Grammar.Name Named(string source)
    {
        Lexer lexer = new(source);
        Parser parser = new(lexer.Lex());

        var datum = Assert.IsType<Ronin.Grammar.Datum>(parser.Parse().Scopes[0].Statements[0]);

        return datum.Identifier.Single().AsName;
    }

    [Fact(DisplayName = "the same name spelled two ways is declared once")]
    public void TheSameNameSpelledTwoWaysIsDeclaredOnce()
    {
        // Four declarations of two names, and shadowing saw none of it: the
        // table held four different raw strings. Resolution canonicalises either
        // spelling back to the single-space form, so the odd one out was a
        // declaration no program could ever reach.
        var compilation = Compilation.Of(new SourceText("""
                                                        var ready part of world => Number;
                                                        var ready part  of world => Number;
                                                        var ready for each value => Number;
                                                        var ready for	each value => Number;

                                                        """, "P.ron"));

        Assert.Equal(2, compilation.Findings.Count);
        Assert.All(compilation.Findings, finding => Assert.IsType<Shadowed>(finding));

        // «old X» is injected for each, so two names are four
        Assert.Equal(4, compilation.Declarations.Symbols.Names.Count);
    }

    [Fact(DisplayName = "a name containing pattern glue is refused, composite keyword and all")]
    public void ANameContainingPatternGlueIsRefusedCompositeKeywordAndAll()
    {
        // R5, bypassed by ordinary source. The glue segment «part of» is ONE
        // word, and the rule compared it against a name split on spaces — so it
        // was matched against «part» and «of», matched neither, and a name
        // containing the glue was declared with nothing said. That is the rule
        // whose entire job is stopping silent capture.
        var finding = Assert.Single(Compilation.Of(new SourceText("""
                                                                  var hello part of alice => Number;
                                                                  function send (x => Number) via part of (y => Number) { return x; }

                                                                  """, "P.ron")).Findings);

        var glue = Assert.IsType<GlueInName>(finding);

        Assert.Equal("hello part of alice", glue.Name);
        Assert.Equal("part of", glue.Word);
    }

    [Theory(DisplayName = "a keyword may not lead a declaration, and may do anything else")]
    [InlineData("if")]
    [InlineData("while")]
    [InlineData("part of")]
    [InlineData("for each")]
    [InlineData("var")]
    public void AKeywordMayNotLeadADeclarationAndMayDoAnythingElse(string keyword)
    {
        // The rule is about the FIRST word of an identifier — that is where a
        // production can steal a declaration, and «function f => Number { … }»
        // becoming a datum named «function f» is what it was written for. It was
        // being applied to every name component instead, so a keyword in GLUE
        // position stopped the identifier dead and the whole declaration came
        // back Malformed.
        Assert.Empty(Compilation.Of(new SourceText($"function send (x => Number) {keyword} (y => Number) {{ return x; }}\n",
                                                   "P.ron")).Findings);

        // and mid-name, which was always allowed and is the same position
        Assert.Empty(Compilation.Of(new SourceText($"var ready {keyword} needed => Number;\n", "P.ron")).Findings);

        // the control: leading, where it still steals
        Assert.NotEmpty(Compilation.Of(new SourceText($"function {keyword} send (x => Number) {{ return x; }}\n",
                                                      "P.ron")).Findings);
    }

    [Fact(DisplayName = "words that cannot be written down are refused, not stored")]
    public void WordsThatCannotBeWrittenDownAreRefusedNotStored()
    {
        // Trivia between the two words of a composite keyword is the one source
        // route to a pattern whose words do not read back as themselves: «part»
        // and «of» are two segments here, and written down they are one. The
        // compiler built one pattern and its own rendering denoted another.
        var finding = Assert.Single(Compilation.Of(new SourceText("""
                                                                  function compute part /* gap */ of (x => Number) { return x; }

                                                                  """, "P.ron")).Findings);

        var unwritable = Assert.IsType<PatternUnwritable>(finding);

        Assert.Equal("«compute» «part» «of» «(_)»", unwritable.Declares);
        Assert.Equal("«compute» «part of» «(_)»", unwritable.Reads);

        // and with the gap closed it is an ordinary declaration
        Assert.Empty(Compilation.Of(new SourceText("function compute part of (x => Number) { return x; }\n",
                                                   "P.ron")).Findings);
    }

    private static IEnumerable<(string Name, string Source)[]> Sequences(int length)
    {
        if (length is 0) return [[]];

        return Sequences(length - 1).SelectMany(_ => Elements, (rest, element) => ((string, string)[])[.. rest, element]);
    }
}
