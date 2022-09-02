using Ronin.Compiler;

namespace Ronin.Tokens.Literals;

internal class UrlLiteral : Token, ILexable<UrlLiteral>
{
    public UrlLiteral(Lexer lexer, int length) : base(lexer, length) { }

    public static UrlLiteral Lex(Lexer lexer)
    {
        var span = lexer.Sourcecode.Span[lexer.Cursor..];

        if (span.Length is < 5) return null;

        int length = 0;
        while (length < span.Length && char.IsLetter(span[length])) ++length;
        if (length == span.Length) 
        {
            lexer.Error = "unterminated url literal";
            return null;
        }

        if (length + 4 >= span.Length || span[length] is not ':' || span[length + 1] is not '/' || span[length + 2] is not '/') return null;
        
        length += 3;
        while (length < span.Length && IsValidChar(span[length])) ++length;
        
        return new UrlLiteral(lexer, length);        
    }

    private static bool IsValidChar(char value) => char.IsLetterOrDigit(value) || value is '~' or '*' or '(' or ')' or '.' or '-' or '_';
}
