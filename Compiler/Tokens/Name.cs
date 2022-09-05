using Ronin.Compiler;

namespace Ronin.Tokens;

internal class Name : Token, ILexable<Name>
{
    public Name(Lexer lexer, int length) : base(lexer, length) { }

    public static Name Lex(Lexer lexer)
    {
        if (lexer.IsEmpty) return null;

        if (char.IsNumber(lexer[0])) return null;

        var length = 0;
        while (length < lexer.Length 
            && !char.IsWhiteSpace(lexer[length]) 
            && lexer[length] is not '(' and not '[' and not '{' and not '}' and not ']' and not ')' and not ';' and not ',' and not '"' and not '\'') ++length;

        return length is 0 ? null : new Name(lexer, length);
    }
}
