// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System;

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

        while (parser.Token is Word or Symbol and not Punctuation) 
        {
            parser.Advance(); 
        }

        if (ReferenceEquals(parser.Token, current.Token)) return null;

        return new Name { Tokens = current.AdvanceTo(ref parser) };
    }
}