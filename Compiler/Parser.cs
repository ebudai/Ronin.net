// Copyright © 2023 Eric Budai

using Ronin.Grammar;
using Ronin.Lexicon;

namespace Ronin.Compiler;

internal interface IParsable<T> where T : IParsable<T>
{
    public static abstract T Parse(ref Parser context);
}

internal struct Parser
{
    public Parser(Token[] tokens) => this.tokens = tokens;

    public List<Statement> Parse()
    {
        List<Statement> statements = new();
        
        while (IsNotFinished)
        {
            if (TriviaSyntax.Parse(ref this) is not null) continue;
            statements.Add(Statement.Parse(ref this));
            if (CurrentToken is Terminal) Advance();
        }

        return statements;
    }

    public List<T> ParseRepeating<T>() where T : class, IParsable<T>
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

    public bool FailsToConsume<T>() where T : Token
    {
        var consumed = CurrentToken is T;
        if (consumed) Advance();
        return consumed is false;
    }

    public ref readonly Token CurrentToken => ref tokens.Span[cursor];
    public ref readonly Token PreviousToken => ref tokens.Span[cursor - 1];

    public readonly ReadOnlySpan<Token> this[Range range] => tokens.Span[range];

    public bool IsNotFinished => CurrentToken is not Sentinel;

    public void Advance() 
    {
        do ++cursor; while (CurrentToken is Trivium);
    }

    public Token[] Commit(scoped ref Parser context)
    {
        var tokens = context[context.cursor..cursor].ToArray();
        context = this;        
        return tokens;
    }

    private readonly ReadOnlyMemory<Token> tokens;
    private int cursor;
}
