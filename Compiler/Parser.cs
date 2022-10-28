using Ronin.Grammar;
using Ronin.Grammar.Declaration;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Ronin.Compiler;

public class Parser
{
    public Parser(Token[] tokens) => _tokens = tokens;

    internal ReadOnlyMemory<Token> Tokens
    {
        get
        {
            var tokens = this[..Location];
            Start += _cursors.Pop();
            return tokens;
        }
    }

    internal int Start { get; set; }    
    internal int Cursor
    {
        get => _cursors.Peek();
        set => _cursors.Push(_cursors.Pop() + value);
    }
    internal int Location => Start + _cursors.Sum();
    
    internal bool IsEmpty => Span.IsEmpty;
    internal bool IsNotEmpty => !IsEmpty;
    internal int Length => Span.Length;

    internal ReadOnlySpan<Token> Span => _tokens[Location..].AsSpan();
    internal ref Token this[int index] => ref _tokens[Location + index];
    internal ReadOnlyMemory<Token> this[Range range] => _tokens[Location..][range];

    internal Syntax[] Parse()
    {
        List<Syntax> statements = new();

        while (Location < _tokens.Length)
        {
            if (IsEmpty) break;

            statements.Add(PartOf.Parse(this)
                ?? Import.Parse(this)
                ?? Datum.Parse(this)
                ?? Trivium.Parse(this)
                ?? Reference.Parse(this)
                ?? Error.Parse(this));

            if (Location < _tokens.Length && _tokens[Location] is Terminal) ++Start;
        }

        return statements.ToArray();
    }

    internal void Reset() => _cursors.Clear();

    private readonly Token[] _tokens;
    private readonly Stack<int> _cursors = new(256);
}
