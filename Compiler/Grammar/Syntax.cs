using Ronin.Compiler;
using Ronin.Token;

namespace Ronin.Grammar;

internal interface IParsable//<T> where T : IParsable<T>
{
    public string Transpile();
    public static abstract Syntax Parse(ref Parser parser);
}

internal abstract class Syntax
{
    protected internal Syntax(Parser parser, int length)
    {
        Tokens.AddRange(parser[..length].ToArray());
        parser.Cursor += length;
    }

    internal Syntax Parent { get; set; }
    
    protected internal readonly List<Lexeme> Tokens = new();

    protected internal record struct Location(int Line, int ColumnStart, int ColumnEnd)
    {
        internal Location(Lexeme token) : this(token.Line, token.Column, token.Column + token.Length) { }
    }
}