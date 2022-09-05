using Ronin.Compiler;

namespace Ronin.Token;

internal class Error : Token
{
    internal Error(Lexer lexer, int length) : base(lexer, length) { }

    internal static Token Lex(Lexer lexer)
    {
        var length = 0;
        while (length < lexer.Length
            && !char.IsWhiteSpace(lexer[length])
            && lexer[length] is not '(' and not '[' and not '{' and not '}' and not ']' and not ')' and not ';' and not ',' and not '"' and not '\'') ++length;

        return new Error(lexer, length);
    }
}
