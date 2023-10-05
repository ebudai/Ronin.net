// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System;

namespace Ronin.Grammar;

/// <summary>
///     Catch-all class for any unparsable <see cref="Token"/>s
/// </summary>
internal class Unknown : Statement, IGrammar<Unknown>
{
    public ReadOnlyMemory<Token> Tokens { get; init; }

    public new static Unknown Parse(ref Parser current)
    {
        Parser parser = current;

        while (parser.Token is not Sentinel and not Terminal and not Separator and not Close)
        {
            parser.Advance();
        }

        if (current == parser) return null;

        return new Unknown { Tokens = current.AdvanceTo(parser) };
    }
}