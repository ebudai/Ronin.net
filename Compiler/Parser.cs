using Ronin.Grammar;
using Ronin.Grammar.Declaration;
using Ronin.Token;
using Ronin.Token.Symbols;

namespace Ronin.Compiler;

public class Parser
{
    public Parser(Lexeme[] tokens)
    {
        Tokens = tokens;
    }

    internal Parser(Parser parser, int advance = 0)
    {
        Tokens = parser.Tokens;
        Cursor = parser.Cursor + advance;
    }

    internal ReadOnlyMemory<Lexeme> Tokens { get; }
    internal int Cursor { get; set; }
    
    internal bool IsEmpty => Span.IsEmpty;
    internal bool IsNotEmpty => !IsEmpty;
    internal int Length => Span.Length;

    internal ReadOnlySpan<Lexeme> Span => Tokens[Cursor..].Span;
    internal Lexeme this[int index] => Span[index];
    internal ReadOnlyMemory<Lexeme> this[Range range] => Tokens[Cursor..][range];

    internal Syntax[] Parse()
    {
        List<Syntax> statements = new();
        
        while (Cursor < Tokens.Length)
        {
            if (IsEmpty) break;

            statements.Add(PartOf.Parse(this)
                ?? Import.Parse(this)
                ?? Datum.Parse(this)
                ?? Trivium.Parse(this)
                ?? Reference.Parse(this)
                ?? Error.Parse(this));

            if (Cursor < Tokens.Length && Tokens.Span[Cursor] is Terminal) ++Cursor;
        }

        return statements.ToArray();
    }

    /*internal Syntax[] Parse<TStart, TEnd>()
    {
        List<Syntax> statements = new();

    }*/

/*    internal bool TryParse<T>(List<Syntax> statements) where T : IParsable
    {
        var syntax = T.Parse(this);
        if (syntax is not null) statements.Add(syntax);
        return syntax is T;
    }*/
}
