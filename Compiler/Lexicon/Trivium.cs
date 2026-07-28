// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using System;

namespace Ronin.Lexicon;

internal abstract class Trivium : Token 
{
    public static Trivium Lex(ref Lexer lexer) => Comment.Lex(ref lexer) ?? Whitespace.Lex(ref lexer) as Trivium;
}

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
    public static class SingleLine
    {
        public const string Start = "//";
    }

    public static class Multiline
    {
        public const string Start = "/*";
        public const string End = "*/";
    }

    public bool Terminated { get; private init; } = true;

    public static new Comment Lex(ref Lexer lexer)
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
            return new Comment { Memory = lexer.AdvanceBy(linelength) };
        }

        if (lexer.StartsWith(Multiline.Start) is false) return null;

        int depth = 1;
        var length = Multiline.Start.Length;

        for (; length != lexer.Length; ++length)
        {
            var innerspan = lexer[length..];
            if (innerspan.StartsWith(Multiline.Start)) ++depth;
            else if (innerspan.StartsWith(Multiline.End)) --depth;
            if (depth is 0) break;
        }

        length += Multiline.End.Length;
        if (depth is not 0 && length > lexer.Length) length = lexer.Length;

        return new Comment { Terminated = depth is 0, Memory = lexer.AdvanceBy(length) };
    }
}

internal class Whitespace : Trivium
{
    internal static new Whitespace Lex(ref Lexer lexer)
    {
        var length = 0;
        while (length < lexer.Length && char.IsWhiteSpace(lexer[length])) ++length;
        if (length is 0) return null;
        return new Whitespace { Memory = lexer.AdvanceBy(length) };
    }
}
