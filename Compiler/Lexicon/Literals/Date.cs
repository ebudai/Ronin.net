using Ronin.Compiler;

namespace Ronin.Lexicon.Literals;

internal class Date : Literal
{
    private Date(Lexer lexer) : base(lexer, 10) { }

    internal static new Token Lex(Lexer lexer)
    {
        if (lexer.Length is < 10) return null;

        if (!char.IsNumber(lexer[0])) return null;
        if (!char.IsNumber(lexer[1])) return null;
        if (!char.IsNumber(lexer[2])) return null;
        if (!char.IsNumber(lexer[3])) return null;
        if (lexer[4] is not '-') return null;
        if (!char.IsNumber(lexer[5])) return null;
        if (!char.IsNumber(lexer[6])) return null;
        if (lexer[7] is not '-') return null;
        if (!char.IsNumber(lexer[8])) return null;
        if (!char.IsNumber(lexer[9])) return null;

        return new Date(lexer);
    }
}
