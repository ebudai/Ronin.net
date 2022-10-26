using Ronin.Compiler;

namespace Ronin.Lexicon.Literals;

internal class Integer : Literal
{
    private Integer(Lexer lexer, int length) : base(lexer, length) { }

    internal static new Token Lex(Lexer lexer)
    {
        if (lexer.IsEmpty || !char.IsNumber(lexer[0])) return null;

        int length = 1;
        for (int max = lexer.Length; length != max; ++length)
        {
            char c = lexer[length];

            if (c is '.' && length + 1 != max) return null; // if the . is at the end of the code, Number won't pick it up

            if (char.IsWhiteSpace(c) || Symbol.IsSymbol(lexer, length)) break;

            if (!char.IsNumber(c) && lexer[length] is not '_') break;
        }

        return new Integer(lexer, length);
    }
}
