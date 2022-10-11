using Ronin.Compiler;

namespace Ronin.Token.Value;

internal class Money : Literal
{
    internal Money(Lexer lexer, int length) : base(lexer, length) { }

    internal static new Lexeme Lex(Lexer lexer)
    {
        if (lexer.IsEmpty || lexer[0] is not '$') return null;

        if (lexer.Length is < 2) return new Error(lexer, lexer.Length, "unterminated money literal");

        if (!char.IsNumber(lexer[1])) return null;

        int length = 2;
        bool hasPeriod = false;
        for (int i = 2, max = lexer.Length; i != max; ++i)
        {
            if (char.IsWhiteSpace(lexer[i]) || Symbol.IsSymbol(lexer, i))
            {
                length = i;
                break;
            }

            if (!char.IsNumber(lexer[i]) && lexer[i] is not '_' and not '.') return new Error(lexer, i, $"money literal with non-numeric character '{lexer[i]}' at {i}");

            if (lexer[i] is '.')
            {
                if (hasPeriod) return new Error(lexer, i, "money literal with multiple dots");
                hasPeriod = true;
            }

            ++length;
        }

        if (lexer[length - 1] is '.') return new Error(lexer, length - 1, "money literal cannot end with a dot");

        return new Money(lexer, length);
    }
}
