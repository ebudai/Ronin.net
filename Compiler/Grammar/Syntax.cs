using Ronin.Compiler;
using Ronin.Token;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Grammar;

internal interface IParsable<T> where T : IParsable<T>
{
    public string Transpile();
    static abstract Syntax Parse(Parser parser);
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

// <THING> is function call | literal | datatype name | compiled datum
// part of thing/stuff with/other things;
// import literal;
// var hit count => integer;
// constant name words = <THING>;
// reactive name words => data type name = <THING>;
// function name words { ... }
// function (first => money, second => time) name words { ... }
// function name words (first => money, second => time) { ... }
// function name words (first => money, second => time) moar name words { ... }
// datatype name words { ... }

// hit count => reactive integer;
// hit count => reactive = max hitpoints - sum from damage select amount;