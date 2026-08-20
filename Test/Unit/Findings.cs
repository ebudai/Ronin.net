// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Runtime;

namespace Unit;

/// <summary>
///     Spans, findings, and the renderer that turns one into a sentence.
/// </summary>
[Trait(nameof(Diagnostics), null)]
public class Findings
{
    /// <summary>
    ///     One of each kind, produced by the rules rather than assembled here.
    /// </summary>
    ///
    /// <remarks>
    ///     This used to build a single finding carrying every role and a related
    ///     span, which meant the golden file showed «first declared here» on a
    ///     reserved-word finding that has no prior site to point at — and no rule
    ///     had ever decided which span was related, because nothing but this
    ///     method populated one. Hand-built data standing in for the real path,
    ///     which is the same fault as a token chain standing in for source.
    /// </remarks>
    private static IEnumerable<Finding> Examples()
    {
        foreach (var source in new[]
                 {
                     "var total => number;\nvar total => number;\n",
                     "var old total => number;\n",
                     """
                     function area of (radius => number) { return radius; }
                     function area of (shape => text) { return shape; }

                     """,
                     """
                     function width of (box => number) { return box; }
                     function width of (frame => number) { return frame; }

                     """,
                     "function old (value => number) { return value; }\n",
                     """
                     function b (x => number) { return x; }
                     function b b (y => number) { return y; }

                     """,
                     "var total otherwise zero => number;\n",
                     "function x otherwise (value => number) { return value; }\n",
                     "var x => = 1;\n",
                     "var provide (x => number) => number;\n",
                     "var cash on hand => money;\n",
                     "var x => number = \"text\";\n",
                     "var z => " + string.Concat(Enumerable.Repeat("optional ", Resolver.MaxLexemes + 1)) + "number;\n",
                     "function " + string.Concat(Enumerable.Repeat("word ", 128)) + "(x => number) {}\n",
                     "function (x => number) rounded { return x; }\n",
                     "function compute part /* gap */ of (x => number) { return x; }\n",
                     "function ping () { return 1; }\n",
                     "function outer (callback (x => number) => number) { return 1; }\n",
                     "function update path { when ready { return; } }\n",
                     "type Box { when ready { return; } }\n",
                     """
                     function item (which => number) of (list => number) { return which; }
                     for each bank in banks { return bank; }

                     """,
                     "var ready => number;\nwhen ready { return 1; }\n",
                     "function odd (x => number) { return; return x; }\n",
                     "function go { stop; }\n",
                     """
                     function send (x => number) { return x; }
                     function send (x => number) to (y => number) { return x; }
                     var a => number;
                     var b => number;
                     var a to b => number;
                     var result = send a to b;

                     """,
                     "function g { return 5; return \"text\"; }\n",
                     "function loop (x) { return loop (x); }\n",
                     "var x => number = nothing;\n",
                 })
        {
            // Through the whole pipeline rather than one phase of it, so an
            // example is what a build would actually print — which is how the
            // parse errors turned out never to be printed at all.
            foreach (var finding in Compilation.Of(new SourceText(source, "Player.ron")).Findings)
            {
                yield return finding;
            }
        }

        // The cascade and initialisation rules take supplied data rather than
        // source, so their examples are supplied too — still produced by the
        // rule, which is what the totality test is asking.
        SourceText nowhere = new(string.Empty);
        Triggering when(string name) => new(name, nowhere.Span(0, 0));

        yield return Cascades.Diagnose(new Dictionary<Triggering, Effects>
        {
            [when("when ping arrives")] = new(new HashSet<string> { "pong count" }, new HashSet<string> { "ping count" }),
            [when("when pong arrives")] = new(new HashSet<string> { "ping count" }, new HashSet<string> { "pong count" }),
        }).Single();

        yield return Cascades.Writers(new Dictionary<Triggering, IReadOnlyCollection<Write>>
        {
            [when("when player dies")] = [new Write("game state", "when player dies")],
            [when("when timer expires")] = [new Write("game state", "when timer expires")],
        }).Single();

        yield return Initialisation.Diagnose(new Dictionary<Declared, IReadOnlySet<string>>
        {
            [new Declared("difficulty", nowhere.Span(0, 0))] = new HashSet<string> { "max health" },
            [new Declared("max health", nowhere.Span(0, 0))] = new HashSet<string> { "difficulty" },
        }).Single();
    }

    [Fact(DisplayName = "every kind is produced by a rule, and has a type of its own")]
    public void EveryKindIsProducedByARuleAndHasATypeOfItsOwn()
    {
        // The message half of this used to be the point: findings carried a
        // dictionary of roles keyed by string, and a producer that misspelled one
        // or left it out threw at report time. A kind's roles are its constructor
        // parameters now, so a finding that cannot be rendered cannot be built —
        // and what is left to check is that each kind is REACHABLE, because a
        // golden file describing something nothing emits is still a fiction.
        var examples = Examples().ToDictionary(finding => finding.Kind);

        var declared = typeof(Finding).Assembly
                                      .GetTypes()
                                      .Where(type => type.IsSubclassOf(typeof(Finding)))
                                      .ToArray();

        foreach (var kind in Enum.GetValues<FindingKind>())
        {
            Assert.True(examples.ContainsKey(kind), $"{kind} is not produced by any rule");

            // one type per kind, so a kind cannot quietly share another's roles
            Assert.Single(declared, type => type.Name == kind.ToString());

            var rendered = Diagnostics.Render(examples[kind]);

            Assert.False(string.IsNullOrWhiteSpace(rendered), $"{kind} renders nothing");
            Assert.DoesNotContain("{", rendered);
        }

        Assert.Equal(Enum.GetValues<FindingKind>().Length, declared.Length);
    }

    [Fact(DisplayName = "a built-in pattern points at no invented declaration")]
    public void ABuiltinPatternPointsAtNoInventedDeclaration()
    {
        // The language supplies «old (_)» without a source declaration, so
        // its zero-width bookkeeping span is not a related location.
        var reserved = Assert.IsType<NameShadowsPattern>(
            Examples().Single(finding => finding.Kind is FindingKind.NameShadowsPattern));

        Assert.True(reserved.Builtin);
        Assert.Empty(reserved.Related);
    }

    [Fact(DisplayName = "a report stays readable at the width of a cascade ring")]
    public void AReportStaysReadableAtTheWidthOfACascadeRing()
    {
        // Built by the cascade rule, from a seven-hop ring. This used to
        // construct an Overloaded finding by hand and count its related lines,
        // which is a test of Report and not of anything about a ring: it did not
        // call Diagnose, contained no ring message, and could not have failed if
        // the ring rule stopped naming every participant.
        var names = Enumerable.Range(0, 7).Select(number => $"when {number} fires").ToArray();
        SourceText source = new(string.Join("\n", names));

        Dictionary<Triggering, Effects> ring = [];

        for (var hop = 0; hop < 7; ++hop)
        {
            // each writes what the next one reads, and the seventh closes on the
            // first
            ring[new Triggering(names[hop], source.Span(source.Text.IndexOf(names[hop], StringComparison.Ordinal), 4))] =
                new(new HashSet<string> { $"cell {hop}" }, new HashSet<string> { $"cell {(hop + 1) % 7}" });
        }

        var finding = Assert.Single(Cascades.Diagnose(ring));

        var lines = Diagnostics.Report(finding).Split(Environment.NewLine);

        // one line for the sentence, then one per other participant — the whole
        // ring named, because naming one of seven is unreadable
        Assert.Equal(7, lines.Length);
        Assert.StartsWith("source:1:1: «when 0 fires» → «when 1 fires» →", lines[0]);
        Assert.Contains("→ «when 6 fires» → «when 0 fires» is a cycle", lines[0]);
        Assert.All(lines.Skip(1), line => Assert.StartsWith("    source:", line));
        Assert.Equal("    source:7:1: also in the ring", lines[^1]);
    }

    [Fact(DisplayName = "the wording of every kind, as a person reads it")]
    public void TheWordingOfEveryKind()
    {
        // The golden file. Rules assert structure, and wording lives here — so
        // improving a message shows up as a reviewable diff rather than as
        // nothing at all, and a rule test does not break when only the English
        // changed.
        var rendered = string.Join(Environment.NewLine + Environment.NewLine,
                                   Examples().Select(Diagnostics.Report));

        Assert.Equal(
            """
            Player.ron:2:5: «total» is already declared in this scope. Shadowing is not allowed, because reading a value has to tell you where it came from, and the compiler cannot flag the ambiguity when both readings are legal. Rename this one.
                Player.ron:1:5: first declared here

            Player.ron:1:5: «old total» cannot be a name: its complete span also reads as a call to the built-in «old (_)», and no bracketing selects the name reading. Rename it; a built-in cannot be respelled.

            Player.ron:1:10: «area of (_)» has 2 declarations and type-directed selection is not implemented, so there is no way to choose between them yet. Give them different shapes for now.
                Player.ron:2:10: also declared here

            Player.ron:1:10: «width of (_)» is declared more than once with the same parameter types, so nothing could ever choose between them. Remove one, or give them different types.
                Player.ron:2:10: also declared here

            Player.ron:1:10: «old (_)» is supplied by the language and cannot be declared again. Respell it.

            Player.ron:2:10: the anchor of «b (_)» begins that of «b b (_)», so a statement can read as either and no bracketing tells them apart. Respell one of them.
                Player.ron:1:10: the anchor it collides with

            Player.ron:1:5: «total otherwise zero» cannot be a name: its complete span also reads as a comparison, because «otherwise» is an operator between two values. No bracketing selects the name reading — a bracket inside the span selects the comparison, and one around it leaves the same two readings inside. Respell it.

            Player.ron:1:10: «x otherwise (_)» uses «otherwise», which the language reads as an operator between two values. A call to it covers the same span as the operation, so every «… otherwise …» in scope would have both readings and no bracketing would tell them apart. Respell it.

            Player.ron:1:1: expected a type after '=>'. «var x => = 1» could not be read, and the rest of the statement was skipped so that one mistake is reported once.

            Player.ron:1:5: «provide (_)» has a parameter list, and only a function may take one. A «var», «let», or «type» is named by words alone — name it without the bracket, or declare it as a function.

            Player.ron:1:21: «money» is not a type. Nothing declares it and the language supplies no such type. Declare it with «type money;», or name a type that exists.

            Player.ron:1:19: This value is a «text», and «number» is declared. A value must have the type its declaration names — change the value, or the type.

            Player.ron:1:10: This type annotation is more than 256 words and symbols, which is past what is read at once. No type is written this large; name one that exists.

            Player.ron:1:10: «word word word word wor ... ord word word word word» has 129 words and holes, and a pattern may have at most 128. Matching one walks a frame per segment, so the limit is what keeps a declaration from being a way to exhaust the stack. Split it into smaller patterns.

            Player.ron:1:10: «(_) rounded» begins with a parameter, which makes it infix rather than a word pattern. A word pattern leads with its name — respell it so the words come first, or declare a symbolic operator, which is where infix belongs.

            Player.ron:1:10: this declares the words «compute» «part» «of» «(_)», and written down they read back as «compute» «part of» «(_)» — a different declaration that spells the same. Two words of a composite keyword have something other than a space between them; close the gap, or respell it.

            Player.ron:1:10: «ping ()» has a bracket with nothing in it. In a NAME, a bracket marks one argument — «send (message) to (recipient)» is called «send x to y» — so «()» is an argument with no name rather than an empty list of them. A function that takes nothing is declared without the brackets. (A delegate is different: its brackets are a signature, and «() => …» is a delegate of no arguments.)

            Player.ron:1:17: «callback (_)» has a bracket in it, and this position takes a name. A bracket marks an argument, and a parameter is bound to one value rather than taking any — so there is nothing for a hole here to mean. Name it, or declare the pattern separately.

            Player.ron:1:24: «when» may only be declared at module scope or inside a type. This one is inside «update path», where it would go out of scope before it could ever run — a step happens between statements, not during one.

            Player.ron:1:12: a «when» inside a type is not implemented yet. Instances are built — one cell per member, and a handle that survives removal — but nothing yet fires a type-scope «when» per instance, so declare it at module scope, or track the instance explicitly.

            Player.ron:1:10: «item (_) of (_)» may not use «of» as glue: «of» is how the compiler builds the injected name «index of «a loop variable»». A pattern that reserves it makes that name illegal everywhere this pattern is in scope. Respell the pattern.

            Player.ron:2:14: «return (_)» in a «when» body — a reaction has nobody to answer. Use «return» to end this firing and leave the «when» in place, or «stop» to remove it.

            Player.ron:1:30: this body both answers and leaves without answering. A body does one or the other — give every «return» a value, or none of them.

            Player.ron:1:15: «stop» removes the «when» it is written in, and there is none here. To leave this body, write «return».

            Player.ron:6:14: this reads 2 ways and the compiler will not choose between them:
                send «a to b»
                send «a» to «b»

            Bracket the one you meant.

            Player.ron:1:31: This return is a «text», and an earlier return is a «number». A function with no written return type takes it from its returns, so they must agree — make them one type, or write it.

            Player.ron:1:28: This return is a call that leads only back into a cycle of returns, never to a value. A recursion, or a group of functions calling only each other, needs a base case that returns a value directly.

            Player.ron:1:19: This value's type is not determined here: «nothing» is an «optional», and «[]» a list, of a type nothing in this position pins. Check it against one that names the inner type — «optional number», «list of number» — or place it beside a value that fixes it.

            source:1:1: «when ping arrives» → «when pong arrives» → «when ping arrives» is a cycle: each writes something the next reads, so firing one schedules the next. Stop one of them writing what the ring reads, or declare feedback on every when in the ring.
                source:1:1: also in the ring

            source:1:1: «game state» is written by 2 whens — «when player dies» and «when timer expires». Whens fire in one round with no order between them, so one write would land and the other vanish. Derive the value instead, with a let that reads both conditions.
                source:1:1: «when timer expires» also writes it

            source:1:1: «difficulty» → «max health» → «difficulty» is a cycle: each initialiser reads the one before it, so none of them can be evaluated first. Break the ring by giving one of them a value that does not depend on the others.
                source:1:1: also in the ring
            """,
            rendered);
    }

    [Fact(DisplayName = "a span knows its line and column")]
    public void ASpanKnowsItsLineAndColumn()
    {
        SourceText source = new("first\nsecond\nthird");

        Assert.Equal((1, 1), source.At(0));
        Assert.Equal((1, 5), source.At(4));
        Assert.Equal((2, 1), source.At(6));
        Assert.Equal((3, 4), source.At(16));

        // and a text with no path still describes itself
        Assert.Equal("source:2:1", source.Span(6, 6).ToString());
    }

    [Fact(DisplayName = "zero length is a position, not a range")]
    public void ZeroLengthIsAPositionNotARange()
    {
        // «expected a type after =>» points between two tokens, and forcing it to
        // cover one would make it point at the wrong character
        SourceText source = new("var x =>;");

        var caret = source.Span(8, 1).At;

        Assert.Equal(0, caret.Length);
        Assert.Equal(8, caret.Offset);
    }

    [Fact(DisplayName = "a span outside its text is refused where it is built")]
    public void ASpanOutsideItsTextIsRefusedWhereItIsBuilt()
    {
        // A malformed span produces a plausible-looking line and column rather
        // than a failure, so the mistake surfaces as a diagnostic pointing
        // somewhere odd — and the thing that computed the offset is nowhere in
        // sight by then. A token offset taken from the wrong source and a length
        // measured in bytes both arrive this way.
        SourceText source = new("first\nsecond");

        Assert.Throws<ArgumentOutOfRangeException>(() => source.Span(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.Span(13, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.Span(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.Span(10, 5));

        // and a length large enough to wrap the addition, which «offset + length
        // > Text.Length» let straight through the check meant to stop it
        Assert.Throws<ArgumentOutOfRangeException>(() => source.Span(1, int.MaxValue));

        // the end of the text is a legal position: it is where «expected a type
        // after «=>»» points when the file simply stops
        Assert.Equal((2, 7), source.At(source.Span(source.Text.Length, 0).Offset));
    }

    [Fact(DisplayName = "a source text with no text is refused at construction")]
    public void ASourceTextWithNoTextIsRefusedAtConstruction()
    {
        // The one guard here that nothing reached. A span means nothing without
        // the text it points into, and letting one be built anyway defers the
        // failure to whichever diagnostic later asks for a line number — which is
        // the furthest possible point from the mistake.
        Assert.Throws<ArgumentNullException>(() => new SourceText(null));

        // a path is genuinely optional: a buffer in an editor has none
        Assert.Null(new SourceText(string.Empty).Path);
    }

    [Fact(DisplayName = "a declaration knows where it was written")]
    public void ADeclarationKnowsWhereItWasWritten()
    {
        // the whole path: source text, tokens carrying offsets, a span, a line
        const string text = "var base price => number;\nvar base price => number;\n";

        SourceText source = new(text, "Player.ron");
        Lexer lexer = new(text);
        Parser parser = new(lexer.Lex());

        var declared = Ronin.Grammar.Declarations.Of(parser.Parse().Scopes[0].Statements, source);
        var problem = Assert.Single(declared.Problems);

        Assert.Equal(FindingKind.Shadowed, problem.Kind);

        // the second declaration, which is the one that has to give way
        Assert.Equal((2, 5), source.At(problem.Primary.Offset));
        Assert.Equal("base price".Length, problem.Primary.Length);
    }
}
