// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

/// <summary>
///     The part of an <see cref="Identifier"/> or <see cref="Reference"/> which is not being used for parameters/arguments
/// </summary>
internal class Name : Syntax, Compiler.IParsable<Name>
{
    public static Name Parse(ref Parser context)
    {
        if (context.CurrentToken is Keyword or Punctuation) return null;

        Parser parser = context;

        while (parser.IsNotFinished)
        {
            if (parser.CurrentToken is not Word and not Symbol or Punctuation) break;
            parser.Advance();
        }

        if (context.CurrentToken == parser.CurrentToken) return null;

        return new Name { Source = parser.Commit(ref context) };
    }
}