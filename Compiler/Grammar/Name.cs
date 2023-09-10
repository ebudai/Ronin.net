// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

/// <summary>
///     The part of an <see cref="Identifier"/> or <see cref="Reference"/> which is not being used for <see cref="Parameters"/> and <see cref="Inputs"/>
/// </summary>
internal class Name : Syntax, IParsableSyntax<Name>
{
    public static Name Parse(ref Parser current)
    {
        Parser parser = current;

        if (parser.Token is not Word and not Symbol or Punctuation) return null;
        
        parser.Advance();

        return new Name { Source = parser.Commit(ref current) };
    }
}