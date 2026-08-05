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
    private static readonly (string Name, string Source)[] elements =
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
    [InlineData("var nested values = [ [ 1 ] [ 2 ] ];\n", "var nested values = [ [ 1 ], [ 2 ] ];\n")]
    [InlineData("var lookup = [ [ 1 ] = [ 2 ] [ 3 ] = [ 4 ] ];\n", "var lookup = [ [ 1 ] = [ 2 ], [ 3 ] = [ 4 ] ];\n")]
    [InlineData("var r = f ([ 1 ] [ 2 ]);\n", "var r = f ([ 1 ], [ 2 ]);\n")]
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

    [Theory(DisplayName = "words that cannot be written down are refused, whatever declares them")]
    [InlineData("var ready part /* gap */ of world => Number;")]
    [InlineData("constant ready part /* gap */ of world => Number;")]
    [InlineData("type ready part /* gap */ of world;")]
    [InlineData("function ready part /* gap */ of world { return 1; }")]
    [InlineData("for each (ready part /* gap */ of world) in banks { return 1; }")]
    public void WordsThatCannotBeWrittenDownAreRefusedWhateverDeclaresThem(string source)
    {
        // An IDENTIFIER's invariant, so every declaration passes through it. It
        // was a pattern's, and reached only by things with a parameter block —
        // so a plain name declared four words, rendered as three, and the symbol
        // table is keyed on the rendering: it held a name whose identity it could
        // not state, agreeing with «var ready part of world» while Name.Equals
        // said the two were different things.
        var finding = Assert.Single(Compilation.Of(new SourceText(source + "\n", "P.ron")).Findings);

        var unwritable = Assert.IsType<UnwritableName>(finding);

        Assert.Equal("«ready» «part» «of» «world»", unwritable.Declares);
        Assert.Equal("«ready» «part of» «world»", unwritable.Reads);

        // and with the gap closed, every one of them is ordinary
        Assert.Empty(Compilation.Of(new SourceText(source.Replace(" /* gap */ ", " ") + "\n", "P.ron")).Findings);
    }

    [Theory(DisplayName = "a declaration at the width bound is refused, never fatal")]
    [InlineData(128, 0, null)]                            // the legal maximum, exactly
    [InlineData(128, 1, FindingKind.UnwritableName)]      // at the maximum, and unreadable
    [InlineData(128, 2, FindingKind.UnwritableName)]
    [InlineData(129, 0, FindingKind.PatternTooWide)]      // one past it
    [InlineData(129, 1, FindingKind.PatternTooWide)]      // both, and width has priority
    [InlineData(129, 2, FindingKind.PatternTooWide)]
    [InlineData(130, 0, FindingKind.PatternTooWide)]
    [InlineData(130, 1, FindingKind.PatternTooWide)]
    [InlineData(130, 2, FindingKind.PatternTooWide)]
    internal void ADeclarationAtTheWidthBoundIsRefusedNeverFatal(int width, int gaps, FindingKind? expected)
    {
        // Both problems at once. The unwritable finding printed what the words
        // read back AS, and worked that out by parsing — which constructs, and
        // the constructor enforces the width bound by THROWING. Reporting one
        // problem crashed on the other, from ordinary source.
        //
        // Width is asked first now, on what was written, and the readback no
        // longer constructs. Either would have been enough; the point of the
        // matrix is that nothing here is fatal.
        // The WIDTH is the input, and the filler is derived from it — the two
        // extra segments are «compute» and the trailing hole, and each
        // interrupted keyword contributes two words rather than one. Counting
        // filler instead meant the rows labelled 128, 129 and 130 exercised
        // 130 to 134: every one comfortably over, so none of them tested the
        // legal maximum and «PatternTooWide» was the answer by construction.
        var filler = width - 2 - (2 * gaps);

        var words = string.Concat(Enumerable.Range(0, filler).Select(each => $"word{each} "));
        var interrupted = string.Concat(Enumerable.Repeat("part /* gap */ of ", gaps));

        var source = $"function compute {interrupted}{words}(x => Number) {{ return x; }}\n";

        var findings = Compilation.Of(new SourceText(source, "P.ron")).Findings;

        // Not «Assert.All», which passes on an empty collection and would have
        // said nothing at all.
        if (expected is null)
        {
            Assert.Empty(findings);
            return;
        }

        Assert.Equal(expected, Assert.Single(findings).Kind);
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

        var unwritable = Assert.IsType<UnwritableName>(finding);

        Assert.Equal("«compute» «part» «of» «(_)»", unwritable.Declares);
        Assert.Equal("«compute» «part of» «(_)»", unwritable.Reads);

        // and with the gap closed it is an ordinary declaration
        Assert.Empty(Compilation.Of(new SourceText("function compute part of (x => Number) { return x; }\n",
                                                   "P.ron")).Findings);
    }

    [Theory(DisplayName = "a parameter is a declaration, and is checked like one")]
    [InlineData("function compute (ready part /* gap */ of world => Number) { return 1; }")]
    [InlineData("var callback = (ready part /* gap */ of world) => { return 1; };")]
    public void AParameterIsADeclarationAndIsCheckedLikeOne(string source)
    {
        // A parameter's identifier reached exactly one thing: «Named», which
        // takes its rendering. So writability, the reserved prefix, collisions,
        // R5 and no-shadowing were every one of them asked of nothing, and a
        // parameter declaring four words was stored under a key stating three.
        var unwritable = Assert.IsType<UnwritableName>(
            Assert.Single(Compilation.Of(new SourceText(source + "\n", "P.ron")).Findings));

        Assert.Equal("«ready» «part» «of» «world»", unwritable.Declares);
        Assert.Equal("«ready» «part of» «world»", unwritable.Reads);
    }

    [Fact(DisplayName = "two parameters that spell the same are one name, and are refused")]
    public void TwoParametersThatSpellTheSameAreOneNameAndAreRefused()
    {
        // The runtime consequence, which is why this is not a diagnostic nicety.
        // Both parameters became the key «ready part of world», binding writes
        // them into a dictionary, and the body received one entry with the first
        // argument silently replaced by the second.
        Assert.NotEmpty(Compilation.Of(new SourceText("""
                                                      function compare (ready part /* gap */ of world => Number,
                                                                        ready part of world => Number) { return 1; }

                                                      """, "P.ron")).Findings);

        // and the plain duplicate, which was equally unreported
        var shadowed = Assert.IsType<Shadowed>(
            Assert.Single(Compilation.Of(new SourceText("function compare (a => Number, a => Number) { return 1; }\n",
                                                        "P.ron")).Findings));

        Assert.Equal("a", shadowed.Name);
    }

    [Theory(DisplayName = "a delegate's parameter is declared into its body, typed or not")]
    [InlineData("var callback = (name) => { var name => Number; return 1; };")]
    [InlineData("var callback = (name => Number) => { var name => Number; return 1; };")]
    public void ADelegatesParameterIsDeclaredIntoItsBodyTypedOrNot(string source)
    {
        // Both spellings declare «name». A delegate's parameter is a bare name
        // when it has no type and a datum when it has one, and one declaration
        // path serves both rather than a second growing beside it with its own
        // idea of the rules.
        var shadowed = Assert.IsType<Shadowed>(
            Assert.Single(Compilation.Of(new SourceText(source + "\n", "P.ron")).Findings));

        Assert.Equal("name", shadowed.Name);
    }

    [Fact(DisplayName = "a parameter is in scope in its body, and shadows nothing there")]
    public void AParameterIsInScopeInItsBodyAndShadowsNothingThere()
    {
        // Parameters were never declared into the body at all — only the loop
        // variable was — so this had nothing to report, and «name» in that body
        // would have quietly read the member instead of the argument.
        var shadowed = Assert.IsType<Shadowed>(Assert.Single(Compilation.Of(new SourceText("""
                                                                                          type Box {
                                                                                              var name => Number;
                                                                                              function read (name => Number) { return name; }
                                                                                          }

                                                                                          """, "P.ron")).Findings));

        Assert.Equal("name", shadowed.Name);
        Assert.Equal("in an enclosing scope", shadowed.Where);
    }

    [Theory(DisplayName = "a delegate body is a scope like any other")]
    [InlineData("var callback = (x) => { var d => Number; var d => Number; };")]
    [InlineData("var handlers = [ (x) => { var d => Number; var d => Number; }, 2 ];")]
    [InlineData("var outer = (x) => { var inner = (y) => { var d => Number; var d => Number; }; };")]
    [InlineData("function run (callback = (x) => { var d => Number; var d => Number; }) { return 1; }")]
    [InlineData("var callback = (x => Number) => { var d => Number; var d => Number; };")]
    public void ADelegateBodyIsAScopeLikeAnyOther(string source)
    {
        // A delegate is a VALUE, so its body could sit in an initialiser, a
        // list, a lookup, an input, a parameter's default or another delegate —
        // and the scope walk was a switch over the statement. Every declaration
        // diagnostic vanished inside one, while the error walk kept finding
        // syntax problems in the same body: the split was invisible because half
        // the diagnostics still worked.
        Assert.Contains(Compilation.Of(new SourceText(source + "\n", "P.ron")).Findings,
                        finding => finding.Kind is FindingKind.Shadowed);
    }

    [Theory(DisplayName = "an empty bracket is a hole with no name")]
    [InlineData("function ping () { return 1; }", "ping ()")]
    [InlineData("function send () to (recipient => Number) { return recipient; }", "send () to (_)")]
    [InlineData("type Box { function ping () { return 1; } }", "ping ()")]
    public void AnEmptyBracketIsAHoleWithNoName(string source, string shape)
    {
        // A bracket in a declaration marks ONE ARGUMENT — «send (message) to
        // (recipient)» is called «send x to y» — so «()» is a hole with no name
        // rather than an empty parameter list, of which Ronin has none.
        //
        // It used to become an ordinary hole: «function ping ()» installed «ping
        // (_)», which «ping» does not resolve against, «ping ()» does not
        // either, and «ping 1» resolves and is then refused at binding. A
        // declaration with no spelling that calls it.
        var empty = Assert.IsType<EmptyHole>(
            Assert.Single(Compilation.Of(new SourceText(source + "\n", "P.ron")).Findings));

        Assert.Equal(shape, empty.Shape);
    }

    [Theory(DisplayName = "a parameter's own brackets are checked, not flattened away")]
    [InlineData("function outer (callback () => Number) { return 1; }", FindingKind.EmptyHole)]
    [InlineData("function outer (() => Number) { return 1; }", FindingKind.EmptyHole)]
    [InlineData("var handler = (a () => Number) => { return 1; };", FindingKind.EmptyHole)]
    [InlineData("function outer (callback (x => Number) => Number) { return 1; }", FindingKind.HoleInName)]
    [InlineData("function outer ((x => Number) rounded => Number) { return 1; }", FindingKind.HoleInName)]
    [InlineData("var handler = (a (x => Number) => Number) => { return 1; };", FindingKind.HoleInName)]
    internal void AParametersOwnBracketsAreCheckedNotFlattenedAway(string source, FindingKind expected)
    {
        // A parameter's identifier is parsed by the general identifier parser,
        // so it can hold holes — and it reached exactly one thing, «Words»,
        // which drops every parameter block. The brackets and the nested
        // declaration disappeared into a runtime name with no finding, and «()»
        // disappeared into the EMPTY STRING as a symbol-table key.
        var compilation = Compilation.Of(new SourceText(source + "\n", "P.ron"));

        Assert.Equal(expected, Assert.Single(compilation.Findings).Kind);

        // and nothing flattened was installed
        Assert.DoesNotContain(string.Empty, compilation.Declarations.Symbols.Names);
        Assert.DoesNotContain("callback", compilation.Declarations.Symbols.Names);
        Assert.DoesNotContain("rounded", compilation.Declarations.Symbols.Names);
    }

    [Theory(DisplayName = "a bare delegate is a delegate, through the real parser")]
    [InlineData("var callback = x => { return x; };")]
    [InlineData("var handlers = [ x => { return x; }, 2 ];")]
    [InlineData("var lookup = [ 1 = x => { return x; } ];")]
    [InlineData("function run (callback = x => { return x; }) { return 1; }")]
    [InlineData("var outer = y => { var inner = x => { return x; }; return 1; };")]
    public void ABareDelegateIsADelegateThroughTheRealParser(string source)
    {
        // «x => { … }» is the documented bare form and its own class's first
        // example, and through Compilation it was Malformed: «Member.Unresolved»
        // accepts «x» as a reference and the alternation commits before anything
        // sees the arrow. The unit test called «Delegate.Parse» directly over a
        // token chain it built itself, so it proved the component while the real
        // path chose a different one.
        Assert.Empty(Compilation.Of(new SourceText(source + "\n", "P.ron")).Findings);
    }

    [Fact(DisplayName = "a bare delegate declares its parameter into its body")]
    public void ABareDelegateDeclaresItsParameterIntoItsBody()
    {
        var shadowed = Assert.IsType<Shadowed>(
            Assert.Single(Compilation.Of(new SourceText("var callback = name => { var name => Number; return 1; };\n",
                                                        "P.ron")).Findings));

        Assert.Equal("name", shadowed.Name);
    }

    [Theory(DisplayName = "an anonymous value keeps what follows it")]
    [InlineData("var r = (x) => { return x; } @ 1;")]      // a delegate, bracketed
    [InlineData("var r = x => { return x; } @ 1;")]        // a delegate, bare
    [InlineData("var r = [ 1, 2 ] @ 1;")]                  // a list
    [InlineData("var r = [ 1 = 2 ] @ 1;")]                 // a lookup
    [InlineData("var r = (1) @ 1;")]                       // an input block
    [InlineData("var vals = [ (x) => { return x; } @ 1 ];")]  // and inside an aggregate
    public void AnAnonymousValueKeepsWhatFollowsIt(string source)
    {
        // §4.8 admits an anonymous value that a symbol continues, and it was
        // never parsed as one: the value won on its own and what followed became
        // a statement of its own. Nothing said they had been separated, because a
        // value IS a statement and the elision makes the wrong split look
        // complete.
        //
        // Not delegate-specific — every anonymous value did it — but a delegate
        // is where it shows, because a bare one begins with a name and so was
        // not even a candidate component.
        Lexer lexer = new(source + "\n");
        Parser parser = new(lexer.Lex());

        Assert.Single(parser.Parse().Scopes[0].Statements);
        Assert.Empty(Compilation.Of(new SourceText(source + "\n", "P.ron")).Findings);
    }

    [Fact(DisplayName = "one parse and one decision, which the spec says and this is why")]
    public void OneParseAndOneDecisionWhichTheSpecSaysAndThisIsWhy()
    {
        // Named for «grammatical-structure.md» §4.7.3-4, so the sentence and the
        // thing that makes it true are findable from each other. The recurring
        // failure here is a claim outliving its evidence: "one parse and one
        // decision" was a design conclusion about what the grammar makes
        // possible, and it went into the spec as a statement about what the
        // compiler does with nothing marking the transition.
        //
        // A RATIO and not a count, because an absolute bakes in today's
        // implementation and gets edited to fit the first time it drifts. A
        // list and a lookup as ordered alternatives cost 2^(d+1) − 2 element
        // attempts against depth; one production costs d.
        static int Work(int depth)
        {
            var source = "var deep = " + new string('[', depth) + " 1 2 " + new string(']', depth) + ";\n";

            Lexer lexer = new(source);
            Parser parser = new(lexer.Lex());

            parser.Parse();

            // The budget resets per file, so this is one parse's work.
            return (int)typeof(Parser).GetField("groups", System.Reflection.BindingFlags.NonPublic
                                                        | System.Reflection.BindingFlags.Static)
                                      .GetValue(null);
        }

        // Twice the depth, under three times the work. Exponential would be
        // about a thousand times: the ordered alternatives cost 2^(d+1) − 2
        // element attempts, and this is d.
        //
        // Two measurements and one comparison, so it is machine-independent and
        // fails on a reintroduction rather than on a refactor. An absolute count
        // would bake in today's implementation and be edited to fit the first
        // time it drifted, which is how a claim outlives its evidence.
        var shallow = Work(10);
        var deep = Work(20);

        Assert.True(deep < shallow * 3, $"depth 10 took {shallow} group attempts and depth 20 took {deep}");
    }

    [Theory(DisplayName = "and asking whether a nest failed costs one walk, not one per level")]
    [InlineData(" 1, 2 ")]
    [InlineData(" 1 2 ")]
    public void AndAskingWhetherANestFailedCostsOneWalkNotOnePerLevel(string inner)
    {
        // Found by audit, and it is the second time this production has lost
        // its curve to a different mechanism. The reflective error walk is
        // priced for running ONCE over a file; the collection classifier asked
        // it as each collection finished, so every level re-descended the level
        // below and a chain of depth d cost 1 + 2 + … + d.
        //
        // BOTH shapes, because the erroneous one hid it: an error short-circuits
        // the walk at the first level, so the malformed nest looked linear while
        // the valid one was quadratic. The probe that guards the other
        // exponential is the malformed shape, which is exactly why it saw
        // nothing.
        long Work(int depth)
        {
            var source = "var deep = " + new string('[', depth) + inner + new string(']', depth) + ";\n";

            Lexer lexer = new(source);
            var tokens = lexer.Lex();

            // Allocation and not time. It is deterministic on one thread, where
            // a stopwatch reports the machine — and the cost here IS allocation:
            // a fresh set and stack per walk, sized by everything the walk
            // reaches.
            var before = GC.GetAllocatedBytesForCurrentThread();

            Parser parser = new(tokens);
            parser.Parse();

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        Work(8);

        var shallow = Work(24);
        var deep = Work(48);

        // Twice the depth, under three times the work. Linear is about twice;
        // the quadratic walk measured 149 KB against 519 KB here, which is
        // three and a half.
        Assert.True(deep < shallow * 3, $"depth 24 allocated {shallow} bytes and depth 48 allocated {deep}");
    }

    [Theory(DisplayName = "and a collection that is half a lookup says so, in either order")]
    [InlineData("var v = [ a = 1, 2 ];")]
    [InlineData("var v = [ 2, a = 1 ];")]
    public void AndACollectionThatIsHalfALookupSaysSoInEitherOrder(string source)
    {
        // SYMMETRICALLY, which is the whole reason the kind is decided after
        // every entry is parsed rather than from the first. Deciding from the
        // first and bailing at the mismatch reports the same mistake two
        // different ways depending on which order it was typed in — and under
        // ordered alternatives it is not merely a worse message but
        // structurally unavailable, because each alternative fails for its own
        // reason and only the last one is seen.
        var finding = Assert.Single(Compilation.Of(new SourceText(source + "\n", "P.ron")).Findings);

        Assert.Equal(FindingKind.Malformed, finding.Kind);
        Assert.Contains("neither a list nor a lookup", finding.Message);
        Assert.Contains("entry 1", finding.Message);
        Assert.Contains("entry 2", finding.Message);
    }

    [Theory(DisplayName = "and a syntax error under an entry is not classified over")]
    [InlineData("var v = [a =, b = 2];")]                 // direct
    [InlineData("var v = [ [a =, b = 2], c = 3];")]       // under a destination
    [InlineData("var v = [ c = 3, [a =, b = 2] ];")]      // under one, beside an association
    [InlineData("var v = [ [1, a =], c = 3];")]           // under a list entry
    [InlineData("var v = [ c = [a =, b = 2], d = 3];")]   // under an origin
    public void AndASyntaxErrorUnderAnEntryIsNotClassifiedOver(string source)
    {
        // Found by audit, twice. The kind of a collection is decided from its
        // entries, and an entry that FAILED has no origin — so a missing
        // association value counted as a plain list value and the whole thing
        // was reported as neither a list nor a lookup, which hides the real
        // mistake and recommends a repair for one nobody made.
        //
        // Asking whether the element itself is an error fixed the direct case
        // and left the same hole one level down. It is asked through the walk
        // the diagnostic pass already uses now, because a shallower test per
        // wrapper is exactly what put the hole there twice.
        Assert.Contains("expected value",
                        Assert.Single(Compilation.Of(new SourceText(source + "\n", "P.ron")).Findings).Message);
    }

    [Theory(DisplayName = "two values with nothing between them are still refused")]
    [InlineData("var v = [ [ 1 ] [ 2 ] ];")]
    [InlineData("var v = [ [ 1 = 2 ] [ 3 ] ];")]
    [InlineData("var r = f ([ 1 ] [ 2 ]);")]
    // and a trailing word does not buy the missing separator
    [InlineData("var v = [ [ 1 ] [ 2 ] name ];")]
    [InlineData("var v = [ (1) (2) name ];")]
    [InlineData("var v = [ [ 1 ] [ 2 ] one two three ];")]
    public void TwoValuesWithNothingBetweenThemAreStillRefused(string source)
    {
        // The other side of the same rule. A reference may lead with an anonymous
        // value — «3..test» does — so the temptation is to admit any run of them,
        // and that would make «{ 1 } { 2 }» one reference and put the aggregate
        // separator rule back where REAUDIT5 found it.
        //
        // It was bought with a trailing word for a while: the run was refused
        // only by a rule asking whether ANY component anywhere was a name, and
        // any word satisfied it. So «{ 1 } { 2 }» was refused and «{ 1 } { 2 }
        // name» was one reference — a missing comma, purchasable.
        Assert.NotEmpty(Compilation.Of(new SourceText(source + "\n", "P.ron")).Findings);
    }

    [Theory(DisplayName = "what may follow a leading value, and what may not")]
    // may: a symbol takes the value as a left operand, indexing included
    [InlineData("var r = [ 1, 2 ] @ 1;", true)]
    [InlineData("var r = [ 1, 2 ] @ 1 @ 1;", true)]
    [InlineData("var r = 3..test;", true)]
    [InlineData("var r = 3 + 4;", true)]
    // and they COMPOSE, which is now one rule rather than two: «@» is a symbol,
    // so an index suffix followed by an operator is a chain of operators
    [InlineData("var r = [ 1, 2 ] @ 1 + 3;", true)]
    [InlineData("var r = [ 1, 2 ] @ 1 @ 1 + 3;", true)]
    [InlineData("var r = [ 1, 2 ] @ 1 + [ 3 ] @ 1;", true)]
    [InlineData("var r = x => { return x; } @ 1 + 3;", true)]
    // may not: a second value is a second value
    [InlineData("var r = [ 1 ] [ 2 ];", false)]
    [InlineData("var r = (1) (2);", false)]
    [InlineData("var r = [ 1 ] (2);", false)]
    public void WhatMayFollowALeadingValueAndWhatMayNot(string source, bool one)
    {
        // A word may be followed by anything — an anonymous value after a word is
        // an ARGUMENT, which is why «thing 7 ("stuff")» has two in a row and is
        // one call. A leading anonymous value is the constrained case, and it was
        // not constrained at all.
        //
        // The composing rows are the ones that were missing. Choosing between the
        // two continuations by looking at the component right after the value
        // could not express an index suffix FOLLOWED by an operator, so the whole
        // expression fell back to the value at its front and the closing-brace
        // elision made the remainder look like another complete statement.
        //
        // Both assertions, and this is why: the split produced zero findings, so
        // asking only for an empty finding list would have certified it.
        Lexer lexer = new(source + "\n");
        Parser parser = new(lexer.Lex());

        var statements = parser.Parse().Scopes[0].Statements;
        var findings = Compilation.Of(new SourceText(source + "\n", "P.ron")).Findings;

        if (one)
        {
            Assert.IsType<Datum>(Assert.Single(statements));
            Assert.Empty(findings);
            return;
        }

        // NOT one reference, which is what this theory is about. What then
        // becomes of the leftover is §4.7's business and not §4.8's: if the
        // value ended with a brace the next statement may start without a
        // terminator — «var r = { 1 } { 2 };» is two statements and legal —
        // and otherwise the module stops and reports what is left.
        Assert.True(statements.Count > 1 || findings.Count > 0,
                    $"«{source}» was read as one reference");
    }

    [Theory(DisplayName = "a word leads, and its arguments may be anything")]
    [InlineData("var r = thing 7 (\"stuff\");")]
    [InlineData("var r = x > 3;")]
    [InlineData("var r = f [0] [1];")]
    [InlineData("var r = f (1) (2);")]
    [InlineData("var r = compute total for order;")]
    public void AWordLeadsAndItsArgumentsMayBeAnything(string source)
    {
        // The positive half, kept beside the negative one so that tightening the
        // sequence cannot quietly erase a valid reference.
        Lexer lexer = new(source + "\n");
        Parser parser = new(lexer.Lex());

        Assert.Single(parser.Parse().Scopes[0].Statements);
        Assert.Empty(Compilation.Of(new SourceText(source + "\n", "P.ron")).Findings);
    }

    [Theory(DisplayName = "a delegate is a name or a bracketed signature, and owns the arrow")]
    [InlineData("var c = x => { return x; };", true)]
    [InlineData("var c = (x) => { return x; };", true)]
    [InlineData("var c = (x => Number) => { return x; };", true)]
    [InlineData("var c = (x, y) => { return x; };", true)]
    [InlineData("var c = (a => Number, b) => { return a; };", true)]
    [InlineData("var c = () => { return 1; };", true)]
    [InlineData("var c = x => Number => { return x; };", false)]   // a bare typed declaration is not one
    public void ADelegateIsANameOrABracketedSignatureAndOwnsTheArrow(string source, bool legal)
    {
        // §4.9.2 as written, because it was written as «datum declaration |
        // parameters => body» — which omits the bare name that is the form most
        // used, admits a bare typed declaration that the parser refuses, and
        // reads as though only the second alternative owns the arrow.
        var findings = Compilation.Of(new SourceText(source + "\n", "P.ron")).Findings;

        if (legal) Assert.Empty(findings);
        else Assert.NotEmpty(findings);
    }

    [Theory(DisplayName = "a statement needs its terminator, at the top of a file too")]
    // separated, or ended by the file
    [InlineData("1; 2;", 2, false)]
    [InlineData("var first = 1;", 1, false)]
    [InlineData("var first = 1", 1, false)]
    // elided, because the statement before already said where it stopped
    [InlineData("function f {} var second = 2;", 2, false)]
    // and a braced statement is the ONLY thing that elides now: a list ends in
    // «]», so «var first = [ 1 ] var second = 2;» is two statements with nothing
    // between them and is refused like any other pair
    [InlineData("var first = x => { return 1; } var second = 2;", 2, false)]
    // and otherwise refused
    [InlineData("1 2;", 1, true)]
    [InlineData("var first = 1 var second = 2;", 1, true)]
    [InlineData("var r = (1) (2);", 1, true)]
    [InlineData("var first = [ 1 ] var second = 2;", 1, true)]
    public void AStatementNeedsItsTerminatorAtTheTopOfAFileToo(string source, int statements, bool refused)
    {
        // A module is a statement sequence and had none of the rule. It TRIED to
        // take a terminator and ignored failing, whatever ended the statement and
        // whatever followed — so «1 2;» was two literals and «var first = 1 var
        // second = 2» was two declarations, with the missing punctuation changing
        // no finding and no declaration.
        //
        // The same tokens inside a block were refused the whole time, so
        // statement validity depended on whether they were in one.
        Lexer lexer = new(source + "\n");
        Parser parser = new(lexer.Lex());

        var module = parser.Parse();

        Assert.Equal(statements, module.Scopes[0].Statements.Count);

        var findings = Compilation.Of(new SourceText(source + "\n", "P.ron")).Findings;

        // Both assertions: the accepted-but-wrong readings produced no findings
        // at all, so an empty-findings check is the failure mode here.
        if (refused) Assert.Equal(FindingKind.Malformed, Assert.Single(findings).Kind);
        else Assert.Empty(findings);
    }

    [Theory(DisplayName = "and the same rule inside a block")]
    [InlineData("function g { 1; 2; }", false)]
    [InlineData("function g { if x { return 1; } return 2; }", false)]
    [InlineData("function g { 1 2; }", true)]
    [InlineData("function g { var first = 1 var second = 2; }", true)]
    public void AndTheSameRuleInsideABlock(string source, bool refused)
    {
        // The pair to the theory above, and the reason the policy is extracted
        // rather than written twice: these two paths are supposed to share a
        // rule, and only one of them had it.
        var findings = Compilation.Of(new SourceText(source + "\n", "P.ron")).Findings;

        if (refused) Assert.Equal(FindingKind.Malformed, Assert.Single(findings).Kind);
        else Assert.Empty(findings);
    }

    private static IEnumerable<(string Name, string Source)[]> Sequences(int length)
    {
        if (length is 0) return [[]];

        return Sequences(length - 1).SelectMany(_ => elements, (rest, element) => ((string, string)[])[.. rest, element]);
    }

    [Theory(DisplayName = "and a lookup may not use one key twice")]
    [InlineData("var v = [ a = 1, a = 2 ];", "«a» is the key of entry 1 and of entry 2")]
    [InlineData("var v = [ a = 1, b = 2, a = 3 ];", "«a» is the key of entry 1 and of entry 3")]
    [InlineData("var v = [ 1 = x, 1 = y ];", "«1» is the key of entry 1 and of entry 2")]
    [InlineData("var v = [ a b = 1, a b = 2 ];", "«a b» is the key of entry 1 and of entry 2")]
    [InlineData("var v = [ a = 1, a = 2, a = 3 ];", "«a» is the key of entry 1 and of entry 2")]
    public void AndALookupMayNotUseOneKeyTwice(string source, string expected)
    {
        // Two entries under one key are two answers with no basis to choose
        // between them — the same shape as a tie, refused for the same reason
        // rather than by silently taking the first or the last.
        //
        // It is also what makes lookup equality answerable at all: with
        // duplicates admitted there is no fact about whether «[a = 1, a = 2]»
        // equals «[a = 2, a = 1]», and either answer is defensible.
        //
        // ONE finding for a key repeated three times, because that is one
        // mistake and three copies of it help nobody.
        var finding = Assert.Single(Compilation.Of(new SourceText(source + "\n", "P.ron")).Findings);

        Assert.Equal(FindingKind.Malformed, finding.Kind);
        Assert.Contains(expected, finding.Message);
    }

    [Theory(DisplayName = "and nothing else in brackets is asked about keys")]
    [InlineData("var v = [ a = 1, b = 2 ];")]
    [InlineData("var v = [ a bc = 1, ab c = 2 ];")]
    [InlineData("var v = [ ab = 1, a b = 2 ];")]
    [InlineData("var v = [ 1, 2, 1 ];")]
    [InlineData("var v = [ a = 1 ];")]
    [InlineData("var v = [ ];")]
    public void AndNothingElseInBracketsIsAskedAboutKeys(string source)
        // A LIST has a null key on every entry, so asking there would call every
        // list of two or more a duplicate — «[1, 2, 1]» repeats a value, which is
        // an ordinary list and nobody's mistake.
        //
        // Found by audit: «a bc» and «ab c» are DIFFERENT keys, and joining
        // their tokens made both «abc» — valid source refused for a collision
        // the compiler invented. Two keys are the same key when they are the
        // same sequence of tokens, and an encoding that forgets where one token
        // ended cannot say that. No separator fixes it either: whatever
        // character is chosen can occur inside a token.
        => Assert.Empty(Compilation.Of(new SourceText(source + "\n", "P.ron")).Findings);
}
