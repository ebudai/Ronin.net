using Ronin.Grammar;
using Ronin.Lexicon;

namespace Ronin.Compiler;

public ref struct Parser
{
    public Parser(Token[] tokens) => _tokens = tokens.AsSpan();

    internal (int index, int length) GetTokens(ref Parser parent)
    {
        var tokens = (_start, Cursor);
        _start += Cursor;
        Cursor = 0;
        parent = this;
        return tokens;
    }

    internal ref readonly Token this[int index] => ref _tokens[Location + index];
    internal ReadOnlySpan<Token> this[Range range] => _tokens[Location..][range];

    internal int Cursor { get; set; }

    internal bool IsEmpty => _tokens.Length <= Location;
    internal bool IsNotEmpty => IsEmpty is not true;
    internal int Length => Span.Length;
    internal ReadOnlySpan<Token> Span => _tokens[Location..];

    public Syntax[] Parse()
    {
        List<Syntax> statements = new();

        while (IsNotEmpty) statements.Add(Statement.Parse(ref this));
        /*{
            statements.Add(PartOf.Parse(ref this)
                ?? Import.Parse(ref this)
                ?? Datum.Parse(ref this)
                ?? Function.Parse(ref this)
                ?? Datatype.Parse(ref this)
                ?? Trivia.Parse(ref this)
                ?? Reference.Parse(ref this)
                ?? Error.Parse(ref this));

            //if (Location < _tokens.Length && _tokens[Location] is Terminal) ++Cursor;
        }*/

        return statements.ToArray();
    }

    internal void AdvancePastTrivia()
    {
        while (this[0] is Trivium) ++_start;
    }

    private int Location => _start + Cursor;

    private readonly ReadOnlySpan<Token> _tokens;
    private int _start = 0;
}
