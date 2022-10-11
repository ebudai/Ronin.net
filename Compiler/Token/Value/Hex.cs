using Ronin.Compiler;

namespace Ronin.Token.Value;

internal class Hex : Literal
{
    public Hex(Lexer lexer, int length) : base(lexer, length) { }

    internal static new Lexeme Lex(Lexer lexer)
    {
        if (lexer.IsEmpty) return null;
        if (lexer[0] is not '0' || lexer[1] is not 'x' and not 'X') return null;

        if (lexer.Length is <= 2) return new Error(lexer, lexer.Length, "unterminated hex literal");

        int length = 2;
        for (int i = 2, max = lexer.Length; i != max; ++i)
        {
            if (char.IsWhiteSpace(lexer[i]) || Symbol.IsSymbol(lexer, i))
            {
                length = i;
                break;
            }

            if (!IsValid(lexer[i]))
            {
                return new Error(lexer, i, $"invalid character '{lexer[i]}' at {i} for hex literal");
            }

            ++length;
        }
        return new Hex(lexer, length);
    }

    private static bool IsValid(char character)
        => char.IsNumber(character)
        || character
        is 'A' or 'a'
        or 'B' or 'b'
        or 'C' or 'c'
        or 'D' or 'd'
        or 'E' or 'e'
        or 'F' or 'f' or '_';
}
