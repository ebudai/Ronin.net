using Ronin.Compiler;
using System.Diagnostics;

namespace Ronin.Lexicon;

internal class Comment : Trivium
{
    internal const string singleline = "//";
    internal const string multilinestart = "/*";
    internal const string multilineend = "*/";

    internal bool Terminated { get; private init; } = true;

    internal Comment(Lexer lexer, int length) : base(lexer, length) { }

    internal static Token Lex(Lexer lexer)
    {
        if (lexer.StartsWith(singleline))
        {
            var linelength = lexer.Span.IndexOf(Environment.NewLine);
            if (linelength is < 0) linelength = lexer.Length;
            return new Comment(lexer, linelength);
        }

        if (!lexer.StartsWith(multilinestart)) return null;

        int depth = 1;
        var length = multilinestart.Length;

        for (; length != lexer.Length; ++length)
        {
            var innerspan = lexer[length..].Span;
            Debug.WriteLine(lexer[length..]);            
            if (innerspan.StartsWith(multilinestart)) ++depth;
            else if (innerspan.StartsWith(multilineend)) --depth;
            if (depth is 0) break;
        }

        length += multilineend.Length;
        var terminated = depth is 0;
        if (terminated is false && length > lexer.Length) length = lexer.Length;

        return new Comment(lexer, length) { Terminated = terminated };
    }
}