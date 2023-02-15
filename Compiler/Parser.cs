using Ronin.Grammar;
using Ronin.Lexicon;

namespace Ronin.Compiler;

internal interface IParsable<T> where T : IParsable<T>
{
    public static abstract T Parse(ref Parser context);
}

internal ref struct Parser
{
    public Parser(Token[] tokens) => this.tokens = tokens;

    public List<Statement> Parse()
    {
        List<Statement> statements = new();
        
        while (IsNotFinished)
        {
            if (Trivia.Parse(ref this) is not null) continue;
            statements.Add(Statement.Parse(ref this));
            if (CurrentToken is Terminal) Advance();
        }

        return statements;
    }

    internal List<T> ParseRepeating<T>() where T : class, IParsable<T>
    {
        List<T> parsed = new();
        while (IsNotFinished)
        {
            var syntax = T.Parse(ref this);
            if (syntax is null) break;
            parsed.Add(syntax);
        }
        return parsed;
    }

    internal bool FailedToConsume<T>() where T : Token
    {
        var consumed = CurrentToken is T;
        if (consumed) Advance();
        return consumed is false;
    }

    internal int Index;

    internal ref readonly Token CurrentToken => ref tokens[Index];

    internal readonly ReadOnlySpan<Token> this[Range range] => tokens[range];
    internal ref readonly Token this[Index index] => ref tokens[index];

    internal bool IsNotFinished => CurrentToken is not Sentinel;

    internal void Advance() 
    {
        do ++Index; while (CurrentToken is Trivium);
    }

    internal Token[] Commit(ref Parser context)
    {
        var tokens = context[context.Index..Index].ToArray();
        context = this;        
        return tokens;
    }

    private readonly ReadOnlySpan<Token> tokens;
}
