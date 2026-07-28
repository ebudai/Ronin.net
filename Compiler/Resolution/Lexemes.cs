// Copyright © 2026 Eric Budai

using Ronin.Lexicon;
using System.Collections.Generic;

namespace Ronin.Compiler;

/// <summary>
///     Adapts the lexer's <see cref="Token"/> linked list to the flat
///     <see cref="Lexeme"/> list that <see cref="Resolver"/> consumes.
/// </summary>
///
/// <remarks>
///     <para>
///     The resolver wants three things, and the lexer already produces all three.
///     One lexeme per word: <c>Word.Lex</c> stops at whitespace, symbols and
///     punctuation, so a multi-word name arrives pre-split and the resolver scores
///     it as a span. Symbols separate from words: they have been their own lexeme
///     class since the lexer was written, which is why <c>Name.Parse</c> no longer
///     re-merges them. Brackets distinguishable from other punctuation:
///     <see cref="Bracket"/> derives from <see cref="Punctuation"/>, so
///     <see cref="Open"/> and <see cref="Close"/> are recognisable by type alone.
///     </para>
///     <para>
///     This is the whole adapter. It classifies and copies text; it does not
///     decide where a statement begins or ends. Callers hand it the tokens of one
///     statement, because <see cref="Resolver.Resolve(IReadOnlyList{Lexeme})"/>
///     scores a span in its entirety and a trailing <c>;</c> is not part of any
///     expression.
///     </para>
/// </remarks>
internal static class Lexemes
{
    /// <summary>Lexes <paramref name="source"/> and adapts the result in one step.</summary>
    public static List<Lexeme> Lex(string source)
    {
        Lexer lexer = new(source);
        return lexer.Lex().ToLexemes();
    }

    /// <summary>
    ///     Walks a token list from <paramref name="head"/> to the
    ///     <see cref="Sentinel"/>. <c>Lexer.Lex</c> discards trivia itself, but
    ///     hand-built token lists in the tests carry <c>Whitespace</c>, so it is
    ///     dropped here too.
    /// </summary>
    public static List<Lexeme> ToLexemes(this Token head)
    {
        List<Lexeme> lexemes = [];
        for (var token = head; token is not null and not Sentinel; token = token.Next as Token)
        {
            if (token is Trivium) continue;
            lexemes.Add(new Lexeme(KindOf(token), token.Memory.ToString()));
        }
        return lexemes;
    }

    /// <remarks>
    ///     Order matters: every case below <see cref="Close"/> would also match it,
    ///     since <c>Open</c> and <c>Close</c> descend from <c>Punctuation</c> and so
    ///     from <c>Symbol</c>.
    /// </remarks>
    private static LexemeKind KindOf(Token token) => token switch
    {
        Open => LexemeKind.Open,
        Close => LexemeKind.Close,

        // a separator divides a group; it is never an operand or an operator
        Separator => LexemeKind.Separator,

        // Date and Text are free atoms for exactly the reason Numeric is: a literal
        // denotes itself, so it costs no symbol table lookup. LexemeKind.Number is
        // named for the only literal the standalone splitter can produce.
        Literal => LexemeKind.Number,

        // Keyword derives from Word. A keyword should never reach the resolver —
        // it is what the parser used to find the statement boundary in the first
        // place — but if one does, treating it as a word makes it fail to resolve
        // rather than silently become an operator.
        Word => LexemeKind.Word,

        _ => LexemeKind.Symbol,
    };
}
