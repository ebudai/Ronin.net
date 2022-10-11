using Ronin.Compiler;

namespace Ronin.Token.Value;

internal class Binary : Literal
{
    public Binary(Lexer lexer, int length) : base(lexer, length) { }

    internal static new Lexeme Lex(Lexer lexer)
    {
        if (lexer.IsEmpty) return null;
        if (lexer[0] is not '0' || lexer[1] is not 'b' and not 'B') return null;

        if (lexer.Length is <= 2) return new Error(lexer, lexer.Length, "unterminated binary literal");

        int length = 2;
        for (int i = 2, max = lexer.Length; i != max; ++i)
        {
            if (lexer[i] is '0' or '1' or '_')
            {
                ++length;
                continue;
            }

            if (char.IsWhiteSpace(lexer[i]) || Symbol.IsSymbol(lexer, i)/* || lexer[i] is Symbol.terminal*/)
            {
                length = i;
                break;
            }

            return new Error(lexer, length, $"invalid char '{lexer[i]}' at {i} for binary literal");
        }

        return new Binary(lexer, length);
    }
}
