// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Lexicon;

/// <summary>
///     Text embedded within the source code which does not participate in compilation
/// </summary>
/// 
/// <remarks>
///     Single-line comments start with // and end with a newline or the eof.  
///     Multi-line comments start with /* and end with */, and if they don't balance, the rest of the file is a comment.
/// </remarks>
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

    public static Comment Lex(ref Lexer lexer)
    {
        if (lexer.StartsWith(SingleLine.Start))
        {
            var linelength = lexer.IndexOf('\n');
            if (linelength is < 0)
            {
                linelength = lexer.Length;
            }
            else if (lexer[linelength - 1] is '\r')
            {
                --linelength;
            }            
            return new Comment { sourcecode = lexer.Commit(linelength) };
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

        return new Comment { Terminated = depth is 0, sourcecode = lexer.Commit(length) };
    }
}