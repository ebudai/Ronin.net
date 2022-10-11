using Ronin.Compiler;

namespace Ronin.Token.Value;

internal class Number : Literal
{
    internal Number(Lexer lexer, int length) : base(lexer, length) { }

    internal static new Lexeme Lex(Lexer lexer)
    {
        if (lexer.IsEmpty || !char.IsNumber(lexer[0])) return null;

        if (lexer.Length is < 3) return new Error(lexer, lexer.Length, "unterminated number literal");

        int length = 0;
        bool hasPeriod = false;
        for (int i = 0, max = lexer.Length; i != max; ++i)
        {
            if (char.IsWhiteSpace(lexer[i]) || Symbol.IsSymbol(lexer, i))
            {
                length = i;
                break;
            }

            if (lexer[i] is '.')
            {
                if (hasPeriod) return new Error(lexer, i, "number literal with multiple dots");
                hasPeriod = true;
            }
            else if (!char.IsNumber(lexer[i]) && lexer[i] is not '_')
            {
                return new Error(lexer, i, $"number literal with non-numeric character '{lexer[i]}' at {i}");
            }

            ++length;
        }

        return new Number(lexer, length);
    }
}
