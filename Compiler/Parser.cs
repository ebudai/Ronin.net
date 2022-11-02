using Ronin.Grammar;
using Ronin.Grammar.Declaration;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Ronin.Compiler;

public struct Parser
{
    public Parser(Token[] tokens) => _tokens = tokens;

    internal ReadOnlyMemory<Token> GetTokens(ref Parser parent)
    {
        var tokens = _tokens[_start..Location];
        _start += Cursor;
        Cursor = 0;
        parent = this;
        return tokens;
    }

    internal ref Token this[int index] => ref _tokens[Location + index];
    internal ReadOnlyMemory<Token> this[Range range] => _tokens[Location..][range];

    internal int Cursor { get; set; }

    internal bool IsNotEmpty => IsEmpty is not true;
    internal int Length => Span.Length;
    internal ReadOnlySpan<Token> Span => _tokens[Location..].AsSpan();

    public Syntax[] Parse()
    {
        List<Syntax> statements = new();

        while (Location < _tokens.Length)
        {
            if (IsEmpty) break;

            statements.Add(PartOf.Parse(ref this)
                ?? Import.Parse(ref this)
                ?? Datum.Parse(ref this)
                ?? Function.Parse(ref this)
                ?? Datatype.Parse(ref this)
                ?? Trivia.Parse(ref this)
                ?? Reference.Parse(ref this)
                ?? Error.Parse(ref this));

            if (Location < _tokens.Length && _tokens[Location] is Terminal) ++Cursor;
        }

        return statements.ToArray();
    }

    internal void AdvancePastTrivia()
    {
        while (this[0] is Trivium) ++_start;
    }

    private bool IsEmpty => Span.IsEmpty;
    private int Location => _start + Cursor;

    private readonly Token[] _tokens;
    private int _start = 0;
}
