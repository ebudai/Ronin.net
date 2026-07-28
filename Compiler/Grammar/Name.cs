// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System;
using System.Linq;

namespace Ronin.Grammar;

/// <summary>
///     The part of an <see cref="Identifier"/> or <see cref="Reference"/> which is not being used for <see cref="Parameters"/> and <see cref="Inputs"/>
/// </summary>
internal class Name
{
    public ReadOnlyMemory<Token> Tokens { get; init; }

    public static Name Parse(ref Parser current)
    {
        Parser parser = current;

        while (parser.Token is Word)
        {
            parser.Advance(); 
        }

        if (ReferenceEquals(parser.Token, current.Token)) return null;

        return new Name { Tokens = current.AdvanceTo(parser) };
    }

    /// <summary>The name as a symbol table holds it: its words, space separated.</summary>
    public string Words => string.Join(' ', Tokens.ToArray().Select(token => token.Memory.ToString()));

    public override bool Equals(object obj) => (obj as Name)?.Tokens.Span.SequenceEqual(Tokens.Span) ?? false;

    /// <remarks>
    ///     Over each token's characters. Hashing <c>Memory</c> itself compared the
    ///     backing object, index and length rather than the contents, so two names
    ///     spelling the same thing hashed differently unless they happened to share
    ///     a string instance — which interning arranged for often enough that the
    ///     test asserting hash equality passed by accident.
    /// </remarks>
    public override int GetHashCode()
    {
        HashCode hashCode = new();
        foreach (var token in Tokens.Span)
        {
            hashCode.Add(token.GetHashCode());
        }
        return hashCode.ToHashCode();
    }
}