// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

namespace Unit;

/// <summary>
///     Where a keyword ends.
/// </summary>
///
/// <remarks>
///     <para>
///     The boundary was whitespace alone, and a word ends at symbols and
///     punctuation too — so a keyword stopped being one whenever punctuation
///     followed it. «var if=&gt;Number» declared a name «if», «type in;»
///     declared a type called «in», and putting a space in changed what the file
///     meant. Every reservation the language makes was one delimiter away from
///     being untrue.
///     </para>
///     <para>
///     Every keyword against every delimiter, because the defect was uniform and
///     a spot check would have found whichever case someone happened to write.
///     </para>
/// </remarks>
[Trait(nameof(Lexer), null)]
public class Boundaries
{
    /// <summary>Every keyword spelling the lexer knows.</summary>
    public static TheoryData<string> Keywords =>
    [
        Ronin.Lexicon.Type.keyword, Ronin.Lexicon.Function.keyword, Variable.keyword, Constant.keyword,
        Let.keyword, Reactive.keyword, Compiled.keyword, Global.keyword, Optional.keyword, Hidden.keyword,
        PartOf.keyword, Ronin.Lexicon.Import.keyword, If.keyword, While.keyword, When.keyword,
        Changing.keyword, ForEach.keyword, In.keyword, Extend.keyword,
    ];

    [Theory(DisplayName = "a keyword ends where a word would")]
    [MemberData(nameof(Keywords))]
    public void AKeywordEndsWhereAWordWould(string keyword)
    {
        // end of input is a boundary — reading one past it to check used to
        // throw for any file ending in one
        Assert.IsAssignableFrom<Keyword>(First(keyword));

        foreach (var after in (string[])[" x", "=>x", "(x", ";", ", x", "{x", "[x", ")", "}", "]", "\n"])
        {
            Assert.IsAssignableFrom<Keyword>(First(keyword + after));
        }
    }

    [Theory(DisplayName = "a keyword with a word carrying on is not a keyword")]
    [MemberData(nameof(Keywords))]
    public void AKeywordWithAWordCarryingOnIsNotAKeyword(string keyword)
    {
        // «iffy» is a word, and so is «types» and «infer» — the boundary has to
        // let a longer word through or the language loses every name that starts
        // with a keyword's letters
        foreach (var carrying in (string[])["y", "1", "er"])
        {
            var token = First(keyword + carrying);

            Assert.IsType<Word>(token);

            // A multi-word keyword is a different case and not a weaker one:
            // «for eachy» is «for» and then «eachy», two ordinary words, because
            // a word ends at the space. What matters either way is that the
            // keyword did not win.
            Assert.Equal(keyword.Contains(' ') ? keyword[..keyword.IndexOf(' ')] : keyword + carrying,
                         token.Memory.ToString());
        }
    }

    [Fact(DisplayName = "a multi-word keyword needs its own single space")]
    public void AMultiWordKeywordNeedsItsOwnSingleSpace()
    {
        // «for each» and «part of» are one token whose spelling contains a
        // space, which is a real constraint and worth pinning: two spaces or a
        // tab is not that keyword, and the reader cannot see the difference.
        Assert.IsType<ForEach>(First("for each x"));
        Assert.IsNotType<ForEach>(First("for  each x"));
        Assert.IsNotType<ForEach>(First("for\teach x"));

        Assert.IsType<PartOf>(First("part of x"));
        Assert.IsNotType<PartOf>(First("part  of x"));
    }

    [Fact(DisplayName = "a reserved word is reserved against punctuation too")]
    public void AReservedWordIsReservedAgainstPunctuationToo()
    {
        // The forms that compiled clean. Each was one delimiter away from the
        // reservation it was supposed to be under.
        foreach (var source in (string[])
                 [
                     "constant in=>Number;\n",
                     "type in;\n",
                     "function in(x=>Number) { return x; }\n",
                     "function f (in=>Number) { return in; }\n",
                     "var if=>Number;\n",
                 ])
        {
            Assert.NotEmpty(Compilation.Of(new SourceText(source, "Player.ron")).Findings);
        }
    }

    [Fact(DisplayName = "a keyword after the first word is an ordinary word")]
    public void AKeywordAfterTheFirstWordIsAnOrdinaryWord()
    {
        // Only «in» is reserved outright. Rejecting every keyword at every
        // position took «ready if needed» and «total function count» out of the
        // language, which was never part of the loop decision.
        foreach (var source in (string[])
                 [
                     "var ready if needed => Number;\n",
                     "var total function count => Number;\n",
                     "function compute while ready (x => Number) { return x; }\n",
                 ])
        {
            Assert.Empty(Compilation.Of(new SourceText(source, "Player.ron")).Findings);
        }

        // and «in» still is not, wherever it appears
        Assert.NotEmpty(Compilation.Of(new SourceText("var ready in waiting => Number;\n", "Player.ron")).Findings);
    }

    private static Token First(string source)
    {
        Lexer lexer = new(source);

        return lexer.Lex();
    }
}
