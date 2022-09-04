using Ronin.Compiler;

namespace Ronin.Tokens.Literals;

internal class UrlLiteral : Token, ILexable<UrlLiteral>
{
    public UrlLiteral(Lexer lexer, int length) : base(lexer, length) { }

    public static UrlLiteral Lex(Lexer lexer)
    {
        if (lexer.Length is < 5) return null;

        int length = 0;
        while (length < lexer.Length && char.IsLetter(lexer[length])) ++length;
        if (length == lexer.Length) 
        {
            lexer.Error = "unterminated url literal";
            return null;
        }

        if (length + 4 >= lexer.Length || lexer[length] is not ':' || lexer[length + 1] is not '/' || lexer[length + 2] is not '/') return null;
        
        length += 3;
        while (length < lexer.Length && IsValidChar(lexer[length])) ++length;
        
        return new UrlLiteral(lexer, length);        
    }

    private static bool IsValidChar(char value) => char.IsLetterOrDigit(value) || value is '~' or '*' or '(' or ')' or '.' or '-' or '_';
}
