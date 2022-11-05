using Ronin.Compiler;

namespace Ronin.Lexicon;

internal class Comment : Trivium
{
    public class SingleLine
    {
        public const string Start = "//";
        
    }

    public class Multiline
    {
        public const string Start = "/*";
        public const string End = "*/";
    }

    public bool Terminated { get; private init; } = true;

    private Comment(Lexer lexer, int length) : base(lexer, length) { }

    public static Token Lex(Lexer lexer)
    {
        if (lexer.StartsWith(SingleLine.Start))
        {
            var linelength = lexer.Span.IndexOf('\n');
            if (linelength is < 0)
            {
                linelength = lexer.Length;
            }
            else if (lexer[linelength - 1] is '\r')
            {
                --linelength;
            }            
            return new Comment(lexer, linelength);
        }

        if (lexer.DoesNotStartWith(Multiline.Start)) return null;

        int depth = 1;
        var length = Multiline.Start.Length;

        for (; length != lexer.Length; ++length)
        {
            var innerspan = lexer[length..].Span;     
            if (innerspan.StartsWith(Multiline.Start)) ++depth;
            else if (innerspan.StartsWith(Multiline.End)) --depth;
            if (depth is 0) break;
        }

        length += Multiline.End.Length;
        if (depth is not 0 && length > lexer.Length) length = lexer.Length;

        return new Comment(lexer, length) { Terminated = depth is 0 };
    }
}