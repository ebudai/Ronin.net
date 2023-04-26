// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

/// <summary>
///     The part of an <see cref="Identifier"/> or <see cref="Reference"/> which is not being used for parameters/arguments
/// </summary>
internal class Name : Syntax, IParsableSyntax<Name>
{
    public static Name Parse(ref Parser current)
    {
        if (current.Token is Reserved or BreakingSymbol) return null;

        Parser parser = current;

        while (parser.IsNotFinished)
        {
            if (parser.Token is not Word and not Symbol or BreakingSymbol) break;
            parser.Advance();
        }

        if (current.Token == parser.Token) return null;

        return new Name { Source = parser.Commit(ref current) };
    }
}