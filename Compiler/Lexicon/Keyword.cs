// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Keyword : Word
{
    /// <summary>
    ///     The spelling a pattern is written with, whatever whitespace the
    ///     author used inside it.
    /// </summary>
    ///
    /// <remarks>
    ///     «for  each» and «for each» are the same keyword, and the lexer says
    ///     so — but a lexeme carried the source slice verbatim, so the resolver
    ///     compared «for  each» against a pattern anchor of «for each» and did
    ///     not match. The grammar accepted the statement and the resolver would
    ///     not read it, which is a split that only shows up when the two are
    ///     joined.
    ///
    ///     Computed rather than stored, because <see cref="Token.Memory"/> is a
    ///     slice of the source and <see cref="Token.Offset"/> is derived from
    ///     where it was cut from — replacing it would move every span.
    /// </remarks>
    public string Canonical
    {
        get
        {
            var text = Memory.Span;

            // Any whitespace at all, not merely a doubled one: a single tab is
            // one character and still not the space the pattern is written with.
            // A single-word keyword has none, so it never allocates.
            foreach (var character in text)
            {
                if (char.IsWhiteSpace(character) is false) continue;

                return string.Join(' ', Memory.ToString()
                                              .Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries));
            }

            return Memory.ToString();
        }
    }

    public static new Word Lex(ref Lexer lexer)
        => Modifier.Lex(ref lexer)
        ?? Mutability.Lex(ref lexer)
        ?? Type.Lex(ref lexer)
        ?? Extend.Lex(ref lexer)
        ?? ForEach.Lex(ref lexer)
        ?? Function.Lex(ref lexer)
        ?? Import.Lex(ref lexer)
        ?? PartOf.Lex(ref lexer)
        ?? If.Lex(ref lexer)
        ?? While.Lex(ref lexer) 
        ?? Changing.Lex(ref lexer)
        ?? When.Lex(ref lexer) as Keyword;

    protected static T Lex<T>(ref Lexer lexer, string keyword) where T : Keyword, new()
    {
        // «for each» and «part of» are one token whose spelling contains a
        // space, and a reader cannot see how many. Every other multi-word thing
        // in this language is a whitespace-insensitive sequence of words, so one
        // construct behaving differently for reasons invisible on screen is a
        // bug report waiting to be filed — «for  each» is «for each».
        if (keyword.Contains(' ')) return Spaced<T>(ref lexer, keyword);

        if (lexer.StartsWith(keyword) is false) return null;

        // A keyword needs a boundary after it, or «iffy» would lex as «if».
        // Reaching the end of the source IS a boundary — reading one past it to
        // check was an IndexOutOfRangeException for a file ending in «if».
        //
        // The boundary is whatever ENDS A WORD, not whitespace alone. Requiring
        // whitespace made a keyword stop being one whenever punctuation followed
        // it: «var if=>Number» declared a name «if», «type in;» declared a type
        // called «in», and adding a space changed what the file meant. Word.Lex
        // stops at symbols and punctuation, so a keyword has to as well or the
        // two disagree about where a token ends.
        if (lexer.Length > keyword.Length && Continues(lexer[keyword.Length])) return null;

        return new T { Memory = lexer.AdvanceBy(keyword.Length) };
    }

    /// <summary>
    ///     A keyword written as several words, separated by any run of
    ///     whitespace rather than by the single space its spelling happens to
    ///     contain.
    /// </summary>
    private static T Spaced<T>(ref Lexer lexer, string keyword) where T : Keyword, new()
    {
        var length = 0;

        foreach (var word in keyword.Split(' '))
        {
            if (length is not 0)
            {
                var spaces = length;
                while (spaces < lexer.Length && char.IsWhiteSpace(lexer[spaces])) ++spaces;

                if (spaces == length) return null;

                length = spaces;
            }

            for (var index = 0; index < word.Length; ++index)
            {
                if (length + index >= lexer.Length || lexer[length + index] != word[index]) return null;
            }

            length += word.Length;
        }

        if (lexer.Length > length && Continues(lexer[length])) return null;

        return new T { Memory = lexer.AdvanceBy(length) };
    }

    /// <summary>
    ///     Whether a character would carry on a word, which is exactly what
    ///     <see cref="Word.Lex"/> consumes.
    /// </summary>
    private static bool Continues(char character)
        => char.IsWhiteSpace(character) is false
        && char.IsSymbol(character) is false
        && char.IsPunctuation(character) is false;
}

internal class Type : Keyword
{
    internal const string keyword = "type";

    public static new Type Lex(ref Lexer lexer) => Lex<Type>(ref lexer, keyword);
}

internal class Extend : Keyword
{
    internal const string keyword = "extend";

    public static new Extend Lex(ref Lexer lexer) => Lex<Extend>(ref lexer, keyword);
}

/// <summary>
///     Opens a loop: «for each bank in banks».
/// </summary>
///
/// <remarks>
///     One token spelling two words, as <see cref="PartOf"/> already is, so that
///     «for» on its own stays an ordinary word — «compute total for order» is a
///     pattern the language wants and reserving «for» would take it away.
/// </remarks>
internal class ForEach : Keyword
{
    internal const string keyword = "for each";

    public static new ForEach Lex(ref Lexer lexer) => Lex<ForEach>(ref lexer, keyword);
}

internal class Function : Keyword
{
    internal const string keyword = "function";

    public static new Function Lex(ref Lexer lexer) => Lex<Function>(ref lexer, keyword);
}

internal class Import : Keyword
{
    internal const string keyword = "import";

    public static new Import Lex(ref Lexer lexer) => Lex<Import>(ref lexer, keyword);
}

internal class PartOf : Keyword
{
    internal const string keyword = "part of";

    public static new PartOf Lex(ref Lexer lexer) => Lex<PartOf>(ref lexer, keyword);
}

internal class If : Keyword
{
    internal const string keyword = "if";

    public static new If Lex(ref Lexer lexer) => Lex<If>(ref lexer, keyword);
}

internal class When : Keyword
{
    internal const string keyword = "when";

    public static new When Lex(ref Lexer lexer) => Lex<When>(ref lexer, keyword);
}

internal class While : Keyword
{
    internal const string keyword = "while";

    public static new While Lex(ref Lexer lexer) => Lex<While>(ref lexer, keyword);
}

internal class Changing : Keyword
{
    internal const string keyword = "changing";

    public static new Changing Lex(ref Lexer lexer) => Lex<Changing>(ref lexer, keyword);
}
