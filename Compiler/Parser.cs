// Copyright © 2023 Eric Budai

using OneOf;
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

    public static bool operator ==(Parser left, Parser right) => left.cursor == right.cursor;
    public static bool operator !=(Parser left, Parser right) => left.cursor != right.cursor;

    public Context Parse()
    {
        List<Statement> statements = new();

        while (IsNotFinished)
        {
            if (Trivia.Parse(ref this) is not null) continue;
            statements.Add(Statement.Parse(ref this));
            if (Token is Terminal) Advance();
        }

        Context context = new();
        context.AddRange(statements);
        return context;
    }

    public List<T> ParseRepeating<T>() where T : IGrammar<T>
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

    public bool TryAdvanceMany<T>() where T : Token
    {
        bool advanced = false;
        while (TryAdvance<T>()) advanced = true;
        return advanced;
    }

    public T TryParse<T>() where T : Token
    {
        if (Token is not T value) return null;
        Advance();
        return value;
    }
    
    public ReadOnlyMemory<Token> AdvanceTo(Parser parser)
    {
        var commit = tokens[cursor..parser.cursor];
        cursor = parser.cursor;
        return commit;
    }

    private readonly ReadOnlyMemory<Token> tokens;
    private int cursor;
}