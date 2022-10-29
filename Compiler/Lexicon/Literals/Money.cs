using Ronin.Compiler;

namespace Ronin.Lexicon.Literals;

internal class Money : Literal
{
    private Money(Lexer lexer, int length) : base(lexer, length) { }

    internal static new Token Lex(Lexer lexer)
    {
        if (lexer.Length is < 2
            || lexer[0] is not '$' 
            || char.IsNumber(lexer[1]) is false) return null;

        int length = 2;
        bool hasPeriod = false;
        for (int max = lexer.Length; length != max; ++length)
        {
            char c = lexer[length];
            if (char.IsWhiteSpace(c) || Symbol.IsNonTerminalSymbol(lexer, length)) break;

            if (char.IsNumber(c) is false && lexer[length] is not '_' and not '.') break;

            if (c is '.')
            {
                if (hasPeriod) break;
                hasPeriod = true;
            }
        }

        if (lexer[length - 1] is '.') --length;

        return new Money(lexer, length);
    }
}
