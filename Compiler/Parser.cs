using Ronin.Grammar;
using Ronin.Token;

namespace Ronin.Compiler;

internal class Parser
{
    internal Parser(Lexeme[] tokens)
    {
        Tokens = tokens;
    }

    internal ReadOnlyMemory<Lexeme> Tokens { get; }
    internal int Cursor { get; set; }
    internal bool IsEmpty => Span.IsEmpty;
    internal int Length => Span.Length;

    internal ReadOnlySpan<Lexeme> Span => Tokens[Cursor..].Span;
    internal Lexeme this[int index] => Span[index];
    internal ReadOnlyMemory<Lexeme> this[Range range] => Tokens[Cursor..][range];

    internal Syntax[] Parse()
    {
        List<Syntax> statements = new();

        while (Cursor < Tokens.Length)
        {
            Syntax statement = PartOf.Parse(this)
                ?? Import.Parse(this)
                ?? throw new Exception($"unknown syntax at {this[0].Line}:{this[0].Column}");
            statements.Add(statement);
        }

        return statements.ToArray();
    }

    internal class Exception : System.Exception
    {
        internal Exception(string message) : base(message) { }
    }
}
