// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class TextLiteral : Literal
{
    public static new Token Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not TextDelimiterSymbol.character) return null;

        var index = 1;
        var length = lexer[index..].Span.IndexOf(TextDelimiterSymbol.character);
        if (length is < 0) return null;

        while (lexer[index + length - 1] is '\\' && length < lexer.Length && length is not -1)
        {
            index += length + 1;
            length = lexer[index..].Span.IndexOf(TextDelimiterSymbol.character);
        }

        if (length is < 0) return null;

        length += index + 1;

        return new TextLiteral { sourcecode = lexer.Commit(length) };
    }
}
