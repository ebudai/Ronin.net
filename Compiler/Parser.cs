using Ronin.Grammar;
using Ronin.Grammar.Declaration;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Ronin.Compiler;

public class Parser
{
    public Parser(Token[] tokens) => Tokens = tokens;

    internal Parser(Parser parser, int advance = 0)
    {
        Tokens = parser.Tokens;
        Cursor = parser.Cursor + advance;
    }

    internal Token[] Tokens { get; set; }
    internal int Cursor { get; set; }
    
    internal bool IsEmpty => Span.IsEmpty;
    internal bool IsNotEmpty => !IsEmpty;
    internal int Length => Span.Length;

    internal ReadOnlySpan<Token> Span => Tokens[Cursor..].AsSpan();
    internal Token this[int index] => Span[index];
    internal ReadOnlyMemory<Token> this[Range range] => Tokens[Cursor..][range];

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

            if (Cursor < Tokens.Length && Tokens[Cursor] is Terminal) ++Cursor;
        }

        return statements.ToArray();
    }
}
