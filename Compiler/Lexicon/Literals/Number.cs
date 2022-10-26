using Ronin.Compiler;

namespace Ronin.Lexicon.Literals;

internal class Number : Literal
{
    private Number(Lexer lexer, int length) : base(lexer, length) { }

    internal static new Token Lex(Lexer lexer)
    {
        if (lexer.Length is < 3 || !char.IsNumber(lexer[0])) return null;

        int length = 1;
        bool hasPeriod = false;
        for (int max = lexer.Length; length != max; ++length)
        {
            char c = lexer[length];

            if (char.IsWhiteSpace(c) || Symbol.IsSymbol(lexer, length)) break;
            if (!char.IsNumber(c) && c is not '_' and not '.') break;

            if (c is '.')
            {
                if (hasPeriod) break;
                hasPeriod = true;
            }
        }

        return hasPeriod ? new Number(lexer, length) : null;
    }
}
