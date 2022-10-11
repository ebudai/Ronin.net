using Ronin.Compiler;
using Ronin.Token.Delimiter;

namespace Ronin.Token.Value;

internal class Character : Literal
{
    public Character(Lexer lexer, int length) : base(lexer, length) { }

    internal static new Lexeme Lex(Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not CharacterDelimiter.character) return null;

        var length = lexer[1..].Span.IndexOf(CharacterDelimiter.character); // find the closing delimiter one

        if (length is < 0) return new Error(lexer, lexer.Length, "unterminated character literal");

        if (length is 0) return new Error(lexer, 2, "empty character literal");

        if (length is not 1 and not 6) return new Error(lexer, length + 2, "bad unicode literal");

        if (length is 6)
        {
            for (var i = 3; i != length; ++i)
            {
                if (!IsValid(lexer[i]))
                {
                    return new Error(lexer, i, $"invalid character '{lexer[i]}' at {i} for unichar literal");
                }
            }
        }

        return new Character(lexer, length + 2);
    }

    private static bool IsValid(char character) 
        => char.IsNumber(character) 
        || character 
        is 'A' or 'a'
        or 'B' or 'b'
        or 'C' or 'c'
        or 'D' or 'd'
        or 'E' or 'e'
        or 'F' or 'f';
}
