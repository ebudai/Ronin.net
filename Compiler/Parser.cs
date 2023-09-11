// Copyright © 2023 Eric Budai

using Ronin.Grammar;
using Ronin.Lexicon;
using System;
using System.Collections.Generic;

namespace Ronin.Compiler;

internal struct Parser
{
    public Parser(List<Token> tokens) => this.tokens = tokens.AsMemory();

    public readonly ref readonly Token Token => ref tokens.Span[cursor];
    
    public readonly bool IsNotFinished => Token is not Sentinel;

    public Context Parse()
    {
        List<Statement> statements = new();

        while (IsNotFinished)
        {
            if (Trivia.Parse(ref this) is not null) continue;
            statements.Add(Statement.Parse(ref this));
            if (Token is Terminal) Advance();
        }

        Context context = new() { Source = tokens };
        context.AddRange(statements);
        return context;
    }

    public List<T> ParseRepeating<T>() where T : IParsableSyntax<T>
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

    public Token Advance()
    {
        do ++cursor; while (Token is Trivium);
        return Token;
    }

    public bool TryAdvance<T>() where T : Token
    {
        var advanced = Token is T;
        if (advanced) Advance();
        return advanced;
    }

    public readonly ReadOnlyMemory<Token> Commit(ref Parser current)
    {
        var tokens = this.tokens[current.cursor..cursor];
        current = this;
        return tokens;
    }

    private readonly ReadOnlyMemory<Token> tokens;
    private int cursor;
}