// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon.Symbols;

namespace Ronin.Lexicon;

internal class TextLiteral : Literal
{
    public static new Token Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not TextDelimiter.symbol) return null;

        for (var i = 1; i < lexer.Length; ++i)
        {
            if (lexer[i] is TextDelimiter.symbol && lexer[i - 1] is not '\\') return new TextLiteral { sourcecode = lexer.Commit(i + 1) };
        }

        return null;
    }
}
