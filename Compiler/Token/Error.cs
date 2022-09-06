using Ronin.Compiler;

namespace Ronin.Token;

internal class Error : Token
{
    internal Error(Lexer lexer, int length, string message = "unparsable token") : base(lexer, length) => Message = message;

    internal string Message { get; }

    internal static Token Lex(Lexer lexer)
    {
        var length = 0;
        while (length < lexer.Length
            && !char.IsWhiteSpace(lexer[length])
            && lexer[length] is not '(' and not '[' and not '{' and not '}' and not ']' and not ')' and not ';' and not ',' and not '"' and not '\'') ++length;

        return new Error(lexer, length);
    }
}
