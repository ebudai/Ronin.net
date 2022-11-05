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

    internal ref readonly Token this[int index] => ref (Location + index >= _tokens.Length) ? ref _finished : ref _tokens[Location + index];

    private static readonly Token _finished = new Finished();

    internal int Cursor { get; set; }

    internal bool IsEmpty => _tokens.Length <= Location;
    internal bool IsNotEmpty => IsEmpty is not true;

    public Syntax[] Parse()
    {
        List<Syntax> statements = new();

        while (IsNotEmpty)
        {
            var statement = Statement.Parse(ref this);
            if (statement is Error error)
            {
                _start = error.Tokens.index;
                Cursor = error.Tokens.length;
            }
            statements.Add(statement);
        }

        return statements.ToArray();
    }

    internal void AdvancePastTrivia()
    {
        while (IsNotEmpty && this[0] is Trivium) ++Cursor;
    }

    internal class Finished : Token
    {
        public Finished() : base(_lexer, 0) { }

        private static readonly Lexer _lexer = new(string.Empty);
    }

    private int Location => _start + Cursor;

    private readonly ReadOnlySpan<Token> _tokens;
    private int _start = 0;
}
