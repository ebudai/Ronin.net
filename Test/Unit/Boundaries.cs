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
        Changing.keyword, ForEach.keyword, Extend.keyword,
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

    [Fact(DisplayName = "a multi-word keyword is separated by whitespace, not by one space")]
    public void AMultiWordKeywordIsSeparatedByWhitespaceNotByOneSpace()
    {
        // «for each» and «part of» are one token whose SPELLING contains a
        // single space, and a reader cannot see how many are there. Every other
        // multi-word thing in this language is a whitespace-insensitive sequence
        // of words, so matching the spelling literally made one construct behave
        // differently for a reason invisible on screen.
        foreach (var spacing in (string[])[" ", "  ", "\t", " \t ", "\n"])
        {
            Assert.IsType<ForEach>(First($"for{spacing}each x"));
            Assert.IsType<PartOf>(First($"part{spacing}of x"));
        }

        // it still needs SOME whitespace, or «foreach» would be the keyword
        Assert.IsNotType<ForEach>(First("foreach x"));
        Assert.IsNotType<PartOf>(First("partof x"));

        // and the source can end in the middle of one, which is a word and not
        // half a keyword
        Assert.IsType<Word>(First("for "));
        Assert.IsType<Word>(First("part "));

        // and the boundary after it holds, as for any other keyword
        Assert.IsType<ForEach>(First("for each(x"));
        Assert.IsNotType<ForEach>(First("for eachy"));
    }

    [Fact(DisplayName = "a reserved word is reserved against punctuation too")]
    public void AReservedWordIsReservedAgainstPunctuationToo()
    {
        // The forms that compiled clean. Each was one delimiter away from the
        // reservation it was supposed to be under.
        //
        // «in» is not among them any more: it is an ordinary word now, reserved
        // nowhere at all, so «type in;» is a type called «in» and no rule has an
        // opinion about it. The keyword boundary bug was never about «in» —
        // «var if=>Number» is the same defect.
        foreach (var source in (string[])
                 [
                     "var if=>Number;\n",
                     "type if;\n",
                     "function while(x=>Number) { return x; }\n",
                     "constant when=>Number;\n",
                 ])
        {
            Assert.NotEmpty(Compilation.Of(new SourceText(source, "Player.ron")).Findings);
        }
    }

    [Fact(DisplayName = "a keyword after the first word is an ordinary word")]
    public void AKeywordAfterTheFirstWordIsAnOrdinaryWord()
    {
        // No word is reserved outright. Rejecting every keyword at every
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

        // «in» included: it is an ordinary word in every position now, because
        // the loop's hole is pinned and the split needs no reservation to be
        // unambiguous. See LoopSyntax.
        Assert.Empty(Compilation.Of(new SourceText("var ready in waiting => Number;\n", "Player.ron")).Findings);
    }

    private static Token First(string source)
    {
        Lexer lexer = new(source);

        return lexer.Lex();
    }
}
