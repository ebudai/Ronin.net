// Copyright © 2023 Eric Budai

using Ronin.Grammar;
using Ronin.Lexicon;
using System;
using System.Collections.Generic;

namespace Ronin.Compiler;

internal interface IParsable<T> where T : IParsable<T>
{
    static abstract T Parse(ref Parser current);
}

internal struct Parser
{
    public Parser(Token start) => Token = start;

    public Token Token;

    public readonly bool IsNotFinished => Token is not Sentinel;

    public Module Parse() => Module.Parse(ref this);

    public List<T> ParseRepeating<T>() where T : IParsable<T>
    {
        List<T> parsed = [];
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
        do Token = Token.Next as Token; while (Token is Trivium);
    }

    public bool TryAdvance<T>() where T : Token
    {
        var advance = Token is T;
        if (advance)
        {
            Advance();
        }
        return advance;
    }

    public bool TryAdvance<T>(out T token) where T : Token
    {
        token = Token as T;
        if (token is not null)
        {
            Advance();
            return true;
        }
        return false;
    }

    public bool TryAdvanceMany<T>() where T : Token
    {
        bool advanced = false;
        while (TryAdvance<T>()) advanced = true;
        return advanced;
    }
    
    /// <remarks>
    ///     Sized by what is collected rather than by the running index, which
    ///     counts trivia that <see cref="Advance"/> then skips — so a name built
    ///     from a token list containing whitespace used to come back padded with
    ///     nulls.
    /// </remarks>
    public ReadOnlyMemory<Token> AdvanceTo(Parser parser)
    {
        List<Token> tokens = [];

        while (ReferenceEquals(Token, parser.Token) is false)
        {
            tokens.Add(Token);
            Advance();
        }

        return tokens.ToArray();
    }
}