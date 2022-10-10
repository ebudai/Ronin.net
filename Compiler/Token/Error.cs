using Ronin.Compiler;

namespace Ronin.Token;

internal class Error : Lexeme
{
    internal Error(Lexer lexer, int length, string message = "unparsable token") : base(lexer, length) => Message = message;

    internal string Message { get; }

    internal static Error Lex(Lexer lexer)
    {
        var length = 0;
        while (length < lexer.Length
            && !char.IsWhiteSpace(lexer[length])
            && Symbol.IsSymbol(lexer, length)) ++length;

        return new Error(lexer, length);
    }
}
