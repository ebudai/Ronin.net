// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class CharacterLiteral : Literal
{
    public static new Token Lex(ref Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not CharacterDelimiterSymbol.character) return null;

        var length = lexer[1..].Span.IndexOf(CharacterDelimiterSymbol.character); // find the closing delimiter one

        if (length is not 1 and not 6) return null;

        if (length is 6)
        {
            for (var i = 3; i != length; ++i)
            {
                if (IsNotValid(lexer[i])) return null;
            }
        }

        return new CharacterLiteral { sourcecode = lexer.Commit(length + 2) };
    }

    private static bool IsNotValid(char character)
        => char.IsNumber(character) is false
        && character
        is not 'A' and not 'a'
        and not 'B' and not 'b'
        and not 'C' and not 'c'
        and not 'D' and not 'd'
        and not 'E' and not 'e'
        and not 'F' and not 'f';
}
