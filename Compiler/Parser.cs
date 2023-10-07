// Copyright © 2023 Eric Budai

using Ronin.Grammar;
using Ronin.Lexicon;
using System;
using System.Collections.Generic;

namespace Ronin.Compiler;

internal struct Parser
{
    public Parser(Token start) => Token = start;

    public Token Token;

    public readonly bool IsNotFinished => Token is not Sentinel;

    public Scope Parse() => Scope.Parse(ref this);

    public List<T> ParseRepeating<T>() where T : IAggregable<T>
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
        do Token = Token.Next as Token; while (Token is Trivium);
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
    
    public ReadOnlyMemory<Token> AdvanceTo(Parser parser)
    {
        var tokens = new Token[parser.Token.RunningIndex - Token.RunningIndex];
        int i = 0;
        while (ReferenceEquals(Token, parser.Token) is false)
        {
            tokens[i++] = Token;
            Token = Token.Next as Token;
        }
        return tokens;
    }
}