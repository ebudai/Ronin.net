// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

/// <summary>
///     The part of an <see cref="Identifier"/> or <see cref="Reference"/> which is not being used for <see cref="Compound.Parameters"/> and <see cref="Compound.Inputs"/>
/// </summary>
internal class Name : Syntax, IParsableSyntax<Name>
{
    public static Name Parse(ref Parser current)
    {
        if (current.Token is Keyword or Punctuation) return null;

        Parser parser = current;

        while (parser.IsNotFinished)
        {
            if (parser.Token is not Word and not Symbol or Punctuation) break;
            parser.Advance();
            if (parser.PreviousToken is Whitespace or Sentinel) break;
        }

        if (current.Token == parser.Token) return null;

        return new Name { Source = parser.Commit(ref current) };
    }
}