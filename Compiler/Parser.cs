using Ronin.Grammar;
using Ronin.Lexicon;

namespace Ronin.Compiler;

public ref struct Parser
{
    public Parser(Token[] tokens) => this.tokens = tokens;

    public Syntax[] Parse()
    {
        List<Syntax> statements = new();

        while (IsNotFinished)
        {
            var trivia = Trivia.Parse(ref this);
            if (trivia is not null) continue;
            var statement = Statement.Parse(ref this);
            statements.Add(statement);
        }

        return statements.ToArray();
    }

    internal ref readonly Token Current => ref tokens[index];

    internal ref readonly Token this[int index] => ref tokens[this.index + index];
    internal ReadOnlySpan<Token> this[Range range] => tokens[range];

    internal bool IsNotFinished => Current is not Sentinel;

    internal void Advance() 
    {
        do { ++index; } while (Current is Trivium);
    }

    internal SourceLocation[] Commit(ref Parser context)
    {
        var tokens = context[context.index..index];
        List<SourceLocation> sources = new();
        foreach (var token in tokens) sources.Add(token.SourceLocation);
        context.index = index;
        return sources.ToArray();
    }

    private int index;
    private readonly ReadOnlySpan<Token> tokens;
}
