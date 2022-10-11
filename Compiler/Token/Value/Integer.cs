using Ronin.Compiler;

namespace Ronin.Token.Value;

internal class Integer : Literal
{
    internal Integer(Lexer lexer, int length) : base(lexer, length) { }

    internal static new Lexeme Lex(Lexer lexer)
    {
        if (lexer.IsEmpty || !char.IsNumber(lexer[0])) return null;

        int length = 0;
        for (int i = 0, max = lexer.Length; i != max; ++i)
        {
            if (lexer[i] is '.') return null;

            if (char.IsWhiteSpace(lexer[i]) || Symbol.IsSymbol(lexer, i))
            {
                length = i;
                break;
            }

            if (!char.IsNumber(lexer[i]) && lexer[i] is not '_') return new Error(lexer, i, $"integer literal with non-numeric character '{lexer[i]}' at {i}");

            ++length;
        }

        return new Integer(lexer, length);
    }
}
