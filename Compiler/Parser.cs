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

    /// <summary>
    ///     Whether the parser sits where a statement can be resumed from, which
    ///     is as far as recovery ever scans.
    /// </summary>
    public readonly bool IsAtBoundary => Token is Sentinel or Terminal or Separator or Close;

    /// <summary>
    ///     The tokens an error node carries — everything the caller had not
    ///     consumed, through to the end of the statement — with the caller left
    ///     past all of it.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     One place for what every error production had been doing slightly
    ///     differently. Three spellings had grown up: advance the caller to where
    ///     the local parser stopped, rescan from the caller with
    ///     <see cref="Unknown"/>, or — in one case — scan a local copy and never
    ///     assign the caller at all.
    ///     </para>
    ///     <para>
    ///     That last one is why this exists rather than a convention. A
    ///     production returning a node without consuming a token leaves its
    ///     caller exactly where it was, so <c>Module.Parse</c> reparses the same
    ///     token forever and appends a fresh error statement every time round:
    ///     «var +;» was not a hang but an out-of-memory, and a one-line malformed
    ///     file could take a machine down. Recovering and advancing the caller
    ///     are the same act here, which is what makes the other thing
    ///     unspellable.
    ///     </para>
    /// </remarks>
    public static ReadOnlyMemory<Token> Recover(ref Parser current, Parser stopped)
    {
        // to the end of the statement, so that what failed is reported once and
        // the next parse starts at a boundary rather than inside the wreckage
        while (stopped.IsAtBoundary is false) stopped.Advance();

        return current.AdvanceTo(stopped);
    }

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