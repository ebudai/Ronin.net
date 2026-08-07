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
}
