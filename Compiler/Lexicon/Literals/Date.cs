using Ronin.Compiler;

namespace Ronin.Lexicon.Literals;

internal class Date : Literal
{
    public static new Token Lex(ref Lexer lexer)
    {
        if (lexer.Length is < Length) return null;

        if (char.IsNumber(lexer[0]) is not true) return null;
        if (char.IsNumber(lexer[1]) is not true) return null;
        if (char.IsNumber(lexer[2]) is not true) return null;
        if (char.IsNumber(lexer[3]) is not true) return null;
        if (lexer[4] is not '-') return null;
        if (char.IsNumber(lexer[5]) is not true) return null;
        if (char.IsNumber(lexer[6]) is not true) return null;
        if (lexer[7] is not '-') return null;
        if (char.IsNumber(lexer[8]) is not true) return null;
        if (char.IsNumber(lexer[9]) is not true) return null;

        return new Date { sourcecode = lexer.Commit(Length) };
    }

    private const int Length = 10;
}
