// Copyright © 2023 Eric Budai

using Ronin.Grammar;
using Ronin.Grammar.Compound;
using Ronin.Lexicon;
using System.Collections.Concurrent;

namespace Ronin.Compiler;

internal interface IParsableSyntax<T> where T : IParsableSyntax<T>
{
    public static abstract T Parse(ref Parser current);
}

internal struct Parser
{
    public ConcurrentDictionary<Reference, Syntax> Context { get; } = new();

    public Parser(in ReadOnlyMemory<Token> tokens) => this.tokens = tokens;

    public Scope Parse()
    {
        List<Statement> statements = new();

        while (IsNotFinished)
        {
            if (Trivia.Parse(ref this) is not null) continue;
            statements.Add(Statement.Parse(ref this));
            if (Token is Terminal) Advance();
        }

        return new Scope { Values = statements, Source = tokens };
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

    public bool TryConsume<T>() where T : Token
    {
        var consumed = Token is T;
        if (consumed) Advance();
        return consumed;
    }

    public ref readonly Token Token => ref tokens.Span[cursor];
    public ref readonly Token PreviousToken => ref tokens.Span[cursor - 1];

    public readonly ReadOnlySpan<Token> this[Range range] => tokens.Span[range];

    public bool IsNotFinished => Token is not Sentinel;

    public void Advance()
    {
        do ++cursor; while (Token is Trivium);
    }

    public Token[] Commit(scoped ref Parser current)
    {
        var tokens = current[current.cursor..cursor].ToArray();
        current = this;
        return tokens;
    }

    private readonly ReadOnlyMemory<Token> tokens;
    private int cursor;
}
