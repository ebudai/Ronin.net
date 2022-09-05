using Ronin.Compiler;

namespace Ronin.Tokens;

internal class Error : Token, ILexable<Error>
{
    public Error(Lexer lexer, int length) : base(lexer, length) { }

    public static Error Lex(Lexer lexer)
    {
        var length = 0;
        while (length < lexer.Length
            && !char.IsWhiteSpace(lexer[length])
            && lexer[length] is not '(' and not '[' and not '{' and not '}' and not ']' and not ')' and not ';' and not ',' and not '"' and not '\'') ++length;

        return new Error(lexer, length);
    }
}
