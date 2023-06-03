// Copyright © 2023 Eric Budai

using Ronin.Grammar;
using Ronin.Grammar.Compound;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;

namespace Ronin.Compiler;

internal interface IParsableSyntax<T> where T : IParsableSyntax<T>
{
    public static abstract T Parse(ref Parser current);
}

internal struct Parser
{
    public Parser(in ReadOnlyMemory<Token> tokens) => this.tokens = tokens;

    public readonly ref readonly Token Token => ref tokens.Span[cursor];
    public readonly ref readonly Token PreviousToken => ref tokens.Span[cursor - 1];
    public readonly bool IsNotFinished => Token is not Sentinel;

    public readonly ReadOnlySpan<Token> this[in System.Range range] => tokens.Span[range];

    public Definition Parse()
    {
        List<Statement> statements = new();

        while (IsNotFinished)
        {
            if (Trivia.Parse(ref this) is not null) continue;
            statements.Add(Statement.Parse(ref this));
            if (Token is Terminal) Advance();
        }

        return new Definition { Values = statements, Source = tokens };
    }

    public List<T> ParseRepeating<T>() where T : class, IParsableSyntax<T>
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

    public void Advance()
    {
        do ++cursor; while (Token is Trivium);
    }

    public bool TryAdvance<T>() where T : Token
    {
        var advanced = Token is T;
        if (advanced) Advance();
        return advanced;
    }

    public readonly Token[] Commit(scoped ref Parser current)
    {
        var tokens = current[current.cursor..cursor].ToArray();
        current = this;
        return tokens;
    }

    private readonly ReadOnlyMemory<Token> tokens;
    private int cursor;
}
