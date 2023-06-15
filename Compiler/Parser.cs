// Copyright © 2023 Eric Budai

using Ronin.Grammar;
using Ronin.Grammar.Compound;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;
using System.Runtime.InteropServices;

namespace Ronin.Compiler;

internal interface IParsableSyntax<T> where T : IParsableSyntax<T>
{
    public static abstract T Parse(ref Parser current);
}

internal ref struct Parser
{
    public Parser(in List<Token> tokens) => this.tokens = CollectionsMarshal.AsSpan(tokens);

    public readonly ref readonly Token this[in Index index] => ref tokens[index];
    public readonly ReadOnlySpan<Token> this[in System.Range range] => tokens[range];

    public readonly ref readonly Token Token => ref tokens[cursor];
    public readonly ref readonly Token PreviousToken => ref tokens[cursor - 1];
    
    public readonly bool IsNotFinished => Token is not Sentinel;

    public Definition Parse()
    {
        List<Statement> statements = new();

        while (IsNotFinished)
        {
            if (Trivia.Parse(ref this) is not null) continue;
            statements.Add(Statement.Parse(ref this));
            if (Token is Terminal) Advance();
        }

        return new Definition { Values = statements, Source = (0, tokens.Length - 1) };
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

    public readonly (int start, int length) Commit(ref Parser current)
    {
        var cursors = (current.cursor, cursor - current.cursor);
        current = this;
        return cursors;
    }

    private readonly ReadOnlySpan<Token> tokens;
    private int cursor;
}
