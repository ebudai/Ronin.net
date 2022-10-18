using Ronin.Compiler;

namespace Ronin.Token.Literals;

internal class Hex : Literal
{
    private Hex(Lexer lexer, int length) : base(lexer, length) { }

    internal static new Lexeme Lex(Lexer lexer)
    {
        if (lexer.Length is <= 2) return null;
        if (lexer[0] is not '0' || lexer[1] is not 'x' and not 'X') return null;

        int length = 2;
        while (length != lexer.Length && IsValid(lexer[length])) ++length;

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
