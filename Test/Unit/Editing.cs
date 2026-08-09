// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Server;

namespace Unit;

/// <summary>
///     What an editor asks the compiler, and what it gets back.
/// </summary>
///
/// <remarks>
///     <para>
///     This language needs an editor more than most, and not for comfort. A name
///     is a run of words, so where one starts and stops cannot be seen without
///     knowing what is in scope — «send a to b» is one call or two names and the
///     text is identical either way. Hover answers the question that provokes,
///     and it was the designer's own recommendation for it.
///     </para>
///     <para>
///     The transport is not here and has nothing to be here: it reads bytes,
///     writes bytes, and looks a method name up in a switch. What it does with a
///     file is a function from source text to an answer, and that is what these
///     are.
///     </para>
/// </remarks>
[Trait(nameof(Language), null)]
public class Editing
{
    private const string Colliding =
        "function send (x => Number) { return x; }\n" +
        "function send (x => Number) to (y => Number) { return x; }\n" +
        "var a to b => Number;\n" +
        "var a => Number;\n" +
        "var b => Number;\n";

    private static SourceText Source(string source) => new(source, "Player.ron");

    [Fact(DisplayName = "a finding is underlined where it is")]
    public void AFindingIsUnderlinedWhereItIs()
    {
        var reported = Assert.Single(Language.Diagnostics(Source(Colliding + "var result = send a to b;\n")));

        // FROM ZERO, which is the whole of what this layer is for: the compiler
        // counts from one because that is what a person reads and what every
        // message it prints has always said, and an editor counts from zero.
        // Line 6 column 14 as a person reads it.
        Assert.Equal(new Place(5, 13), reported.Extent.From);
        Assert.Equal(new Place(5, 24), reported.Extent.To);

        // The KIND, not a number: a number would be a second registry to keep in
        // step with the first, and this is what an editor dispatches a fix on.
        Assert.Equal("Ambiguous", reported.Code);
        Assert.Contains("send «a to b»", reported.Message);
    }

    [Fact(DisplayName = "and a clean file reports nothing")]
    public void AndACleanFileReportsNothing()
        => Assert.Empty(Language.Diagnostics(Source("var a => Number;\nvar b => Number;\n")));

    [Fact(DisplayName = "hover shows the brackets the compiler inferred")]
    public void HoverShowsTheBracketsTheCompilerInferred()
    {
        // The reading, which is the answer to «what did the compiler think I
        // wrote» — and the only way to see it, because the two readings of these
        // words differ nowhere in the text.
        var source = Source("function send (x => Number) { return x; }\n"
                          + "function send (x => Number) to (y => Number) { return x; }\n"
                          + "var a => Number;\nvar b => Number;\n"
                          + "var result = send a to b;\n");

        Assert.Equal("send «a» to «b»", Language.Hover(source, new Place(4, 14)));

        // Anywhere IN the statement, because the statement is the unit with a
        // reading. Asking about a word would answer about a word, and which
        // words group into one name is the entire question.
        Assert.Equal("send «a» to «b»", Language.Hover(source, new Place(4, 22)));
    }

    [Fact(DisplayName = "and the same words read differently when a name exists")]
    public void AndTheSameWordsReadDifferentlyWhenANameExists()
    {
        // Identical text, one extra declaration, and the statement means
        // something else. Nothing in the file shows that, which is why an editor
        // has to.
        var source = Source("function send (x => Number) to (y => Number) { return x; }\n"
                          + "var a to b => Number;\nvar a => Number;\nvar b => Number;\n"
                          + "var result = send a to b;\n");

        Assert.Equal("send «a» to «b»", Language.Hover(source, new Place(4, 14)));

        Assert.Equal("send «a to b»",
                     Language.Hover(Source("function send (x => Number) { return x; }\n"
                                         + "var a to b => Number;\n"
                                         + "var result = send a to b;\n"),
                                    new Place(2, 14)));
    }

    [Theory(DisplayName = "and there is nothing to say where there is nothing to say")]
    [InlineData(0, 0)]      // a declaration, which has no expression to read
    [InlineData(4, 0)]      // «var result», before the expression begins
    [InlineData(9, 0)]      // past the end of the file
    public void AndThereIsNothingToSayWhereThereIsNothingToSay(int line, int character)
        // An editor showing an empty box over every space is worse than one
        // showing nothing, so absence is the answer rather than an empty string.
        => Assert.Null(Language.Hover(Source(Colliding + "var result = send a to b;\n"),
                                      new Place(line, character)));

    [Fact(DisplayName = "and an ambiguous statement has no reading to show")]
    public void AndAnAmbiguousStatementHasNoReadingToShow()
        // It has several, and picking one to hover would be the silent choice
        // this whole direction removed. The diagnostic lists them, which is
        // where a set of readings belongs.
        => Assert.Null(Language.Hover(Source(Colliding + "var result = send a to b;\n"), new Place(5, 14)));

    [Fact(DisplayName = "a code action offers each reading, and applying it selects that reading")]
    public void ACodeActionOffersEachReadingAndApplyingItSelectsThatReading()
    {
        // Ambiguity is the error that offers the bracketings selectably, and this
        // is where "selectably" becomes real: each reading is a fix an editor can
        // apply, titled by the statement with that fix's brackets typed in —
        // because a person choosing between two bracketings is choosing between
        // two meanings, and the bracketed source is the meaning made visible.
        const string Text = "function send (x => Number) { return x; }\n"
                          + "function send (x => Number) to (y => Number) { return x; }\n"
                          + "var a to b => Number;\nvar a => Number;\nvar b => Number;\n"
                          + "var result = send a to b;\n";

        var actions = Language.Actions(Source(Text), new Extent(new Place(5, 13), new Place(5, 24)));

        Assert.Equal(["Read it as send (a to b)", "Read it as send (a) to b"],
                     actions.Select(action => action.Title));

        // Applying each one leaves a file that compiles — the edits are real, not
        // a description of where an edit would go.
        Assert.All(actions, action => Assert.Empty(Language.Diagnostics(Source(Applied(Text, action)))));
    }

    [Fact(DisplayName = "and two readings that print alike still get titles a person can tell apart")]
    public void AndTwoReadingsThatPrintAlikeStillGetTitlesAPersonCanTellApart()
    {
        // Found by audit. The reading was the title, and these two readings print
        // the same words — «print send «a» to «b»» for both «print (send a to b)»
        // and «print (send a) to b» — so a person saw two working fixes under one
        // label with no way to tell which meaning either selected. The bracketed
        // source is the title now, and a bracketing IS the reading it selects, so
        // the two entries differ exactly where the meanings do.
        const string Text = "function send (x => Number) { return x; }\n"
                          + "function send (x => Number) to (y => Number) { return x; }\n"
                          + "function print (x => Number) { return x; }\n"
                          + "function print (x => Number) to (y => Number) { return x; }\n"
                          + "var a => Number;\nvar b => Number;\nvar result = print send a to b;\n";

        var actions = Language.Actions(Source(Text), new Extent(new Place(6, 13), new Place(6, 30)));

        // Two working fixes, and two titles that name the two meanings apart.
        Assert.Equal(2, actions.Count);
        Assert.Equal(2, actions.Select(action => action.Title).Distinct().Count());
        Assert.All(actions, action => Assert.Empty(Language.Diagnostics(Source(Applied(Text, action)))));

        Assert.Contains(actions, action => action.Title == "Read it as print (send a to b)");
        Assert.Contains(actions, action => action.Title == "Read it as print (send a) to b");
    }

    [Fact(DisplayName = "and a range with no ambiguity in it offers nothing")]
    public void AndARangeWithNoAmbiguityInItOffersNothing()
    {
        // Recomputed from the current text, so a range that is not ambiguous — a
        // declaration, or a statement already bracketed — has no fixes. An action
        // built from a stale diagnostic would insert brackets where the words no
        // longer are.
        var actions = Language.Actions(Source("var a => Number;\nvar b => Number;\n"),
                                       new Extent(new Place(0, 0), new Place(0, 15)));

        Assert.Empty(actions);
    }

    [Theory(DisplayName = "and the actions are the statement the cursor is in, not another")]
    [InlineData(5, 5, 24, true)]    // on the ambiguous statement, line 6
    [InlineData(2, 0, 20, false)]   // a declaration above it
    [InlineData(7, 0, 5, false)]    // past the end
    [InlineData(5, 20, 22, true)]   // a selection wholly inside the statement
    public void AndTheActionsAreTheStatementTheCursorIsInNotAnother(int line, int from, int to, bool offered)
    {
        // A code-action request carries the range under the cursor, and the fixes
        // are the ambiguity that range touches — an ambiguous statement three
        // lines up is not the one a person is looking at. Any overlap counts, so
        // a cursor anywhere in the statement offers its fixes and a cursor in
        // another statement does not.
        var actions = Language.Actions(Source(Colliding + "var result = send a to b;\n"),
                                       new Extent(new Place(line, from), new Place(line, to)));

        Assert.Equal(offered ? 2 : 0, actions.Count);
    }

    [Fact(DisplayName = "and a reading that needs three brackets is still offered as an action")]
    public void AndAReadingThatNeedsThreeBracketsIsStillOfferedAsAnAction()
    {
        // The three-child ambiguity through the editor boundary: eight readings,
        // five shown, and five actions — each a set of three bracket pairs. The
        // two-level search offered nothing selectable for a reading that pins
        // three children at once, so the whole promise still failed for a short
        // statement a person could write.
        const string Text = Colliding + "var result = (send a to b) + (send a to b) + (send a to b);\n";

        var actions = Language.Actions(Source(Text), new Extent(new Place(5, 13), new Place(5, 57)));

        Assert.Equal(5, actions.Count);

        // Five distinct edits, and applying any one leaves a file that compiles.
        Assert.Equal(5, actions.Select(action => Applied(Text, action)).Distinct().Count());
        Assert.All(actions, action => Assert.Empty(Language.Diagnostics(Source(Applied(Text, action)))));
    }

    [Fact(DisplayName = "and a reading containing a list is offered as an action")]
    public void AndAReadingContainingAListIsOfferedAsAnAction()
    {
        // The collection case through the editor boundary: two readings, two
        // actions with distinct edits, each applying to a clean file — where
        // bracketing every subtree, and grouping a list's element the comparison
        // then left bare, offered none.
        const string Text = "function send (x => Number) { return x; }\n"
                          + "function send (x => Number) to (y => Number) { return x; }\n"
                          + "function print (x => Number) { return x; }\n"
                          + "function print (x => Number) to (y => Number) { return x; }\n"
                          + "var a => Number;\nvar b => Number;\nvar result = print send [a] to b;\n";

        var actions = Language.Actions(Source(Text), new Extent(new Place(6, 13), new Place(6, 30)));

        Assert.Equal(2, actions.Count);
        Assert.Equal(2, actions.Select(action => action.Title).Distinct().Count());
        Assert.All(actions, action => Assert.Empty(Language.Diagnostics(Source(Applied(Text, action)))));
    }

    [Fact(DisplayName = "and a reading needing a span every displayed rival shares is still offered")]
    public void AndAReadingNeedingASpanEveryDisplayedRivalSharesIsStillOffered()
    {
        // The capped-alternative case through the editor boundary: sixteen
        // readings, five shown all using the cheaper «send (a to b)», and five
        // actions — each ruling out the «send (a) to b» reading the display cap
        // hid. Comparing a bracket only against the displayed rivals offered four
        // actions or, past the ceiling, none.
        var Text = "function send (n => Number) { return n; }\n"
                 + "function send (n => Number) to (m => Number) { return n; }\n"
                 + "function print (n => Number) { return n; }\n"
                 + "function print (n => Number) to (m => Number) { return n; }\n"
                 + "var a to b => Number;\nvar a => Number;\nvar b => Number;\nvar x => Number;\nvar y => Number;\n"
                 + "var result = (send a to b) + (print send x to y) + (print send x to y) + (print send x to y) + "
                 + string.Join(" + ", Enumerable.Repeat("a", 12)) + ";\n";

        var actions = Language.Actions(Source(Text), new Extent(new Place(9, 13), new Place(9, 60)));

        Assert.Equal(5, actions.Count);
        Assert.Equal(5, actions.Select(action => Applied(Text, action)).Distinct().Count());
        Assert.All(actions, action => Assert.Empty(Language.Diagnostics(Source(Applied(Text, action)))));
    }

    [Fact(DisplayName = "and overlapping patterns are offered one-pair actions, not a surplus pair")]
    public void AndOverlappingPatternsAreOfferedOnePairActionsNotASurplusPair()
    {
        // The overlapping-pattern case through the editor boundary: two readings
        // of «f a with b end» that share «a» and part at the second argument, two
        // actions of ONE pair each — where taking the shared argument first added
        // a surplus pair that, at the ceiling, turned the answer into no action.
        const string Text = "function f (x => Number) with (y => Number) end { return x; }\n"
                          + "function f (x => Number) with (y => Number) { return x; }\n"
                          + "var a => Number;\nvar b => Number;\nvar b end => Number;\n"
                          + "var result = f a with b end;\n";

        var actions = Language.Actions(Source(Text), new Extent(new Place(5, 13), new Place(5, 27)));

        Assert.Equal(2, actions.Count);
        Assert.All(actions, action => Assert.Single(action.Edits, edit => edit.Text == "("));
        Assert.All(actions, action => Assert.Empty(Language.Diagnostics(Source(Applied(Text, action)))));
    }

    [Fact(DisplayName = "and a repeated argument still gets both actions, one pair each")]
    public void AndARepeatedArgumentStillGetsBothActionsOnePairEach()
    {
        // The repeated-name case through the editor boundary: «f a with a end»,
        // where matching arguments by value made the second «a» look shared with
        // the first and dropped an action. Two actions of one pair each now.
        const string Text = "function f (x => Number) with (y => Number) end { return x; }\n"
                          + "function f (x => Number) with (y => Number) { return x; }\n"
                          + "var a => Number;\nvar a end => Number;\n"
                          + "var result = f a with a end;\n";

        var actions = Language.Actions(Source(Text), new Extent(new Place(4, 13), new Place(4, 27)));

        Assert.Equal(2, actions.Count);
        Assert.All(actions, action => Assert.Single(action.Edits, edit => edit.Text == "("));
        Assert.All(actions, action => Assert.Empty(Language.Diagnostics(Source(Applied(Text, action)))));
    }

    /// <summary>The text with a code action's edits applied.</summary>
    private static string Applied(string text, Fix action)
    {
        var lines = text.Split('\n');

        // Right to left, so an earlier edit's column is untouched by a later one.
        foreach (var edit in action.Edits.OrderByDescending(edit => (edit.At.Line, edit.At.Character)))
        {
            var line = lines[edit.At.Line];
            lines[edit.At.Line] = line[..edit.At.Character] + edit.Text + line[edit.At.Character..];
        }

        return string.Join('\n', lines);
    }
}
