using Ronin.Compiler;

namespace Ronin.Lexicon.Literals;

internal class Money : Literal
{
    public static new Token Lex(ref Lexer lexer)
    {
        if (lexer.Length is < 2
            || lexer[0] is not '$' 
            || char.IsNumber(lexer[1]) is false) return null;

        int length = 2;
        bool hasPeriod = false;
        for (int max = lexer.Length; length != max; ++length)
        {
            ref readonly char c = ref lexer[length];

            if (char.IsNumber(c) is false && lexer[length] is not '_' and not '.') break;

            if (c is '.')
            {
                if (hasPeriod) break;
                hasPeriod = true;
            }
        }

        if (lexer[length - 1] is '.') --length;

        return new Money { sourcecode = lexer.Commit(length) };
    }
}
