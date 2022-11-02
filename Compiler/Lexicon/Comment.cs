using Ronin.Compiler;
using Ronin.Lexicon.Symbols;
using System.Diagnostics;

namespace Ronin.Lexicon;

internal class Comment : Trivium
{
    internal bool Terminated { get; private init; } = true;

    internal Comment(Lexer lexer, int length) : base(lexer, length) { }

    internal static Token Lex(Lexer lexer)
    {
        if (lexer.StartsWith(CommentStart.singleline))
        {
            var linelength = lexer.Span.IndexOf('\n');
            if (linelength is < 0) linelength = lexer.Length;
            return new Comment(lexer, linelength);
        }

        if (!lexer.StartsWith(CommentStart.multiline)) return null;

        int depth = 1;
        var length = CommentStart.multiline.Length;

        for (; length != lexer.Length; ++length)
        {
            var innerspan = lexer[length..].Span;
            Debug.WriteLine(lexer[length..]);            
            if (innerspan.StartsWith(CommentStart.multiline)) ++depth;
            else if (innerspan.StartsWith(CommentEnd.multiline)) --depth;
            if (depth is 0) break;
        }

        length += CommentEnd.multiline.Length;
        if (depth is not 0 && length > lexer.Length) length = lexer.Length;

        return new Comment(lexer, length) { Terminated = depth is 0 };
    }
}