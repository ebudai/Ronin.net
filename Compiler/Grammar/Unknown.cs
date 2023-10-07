// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System;

namespace Ronin.Grammar;

/// <summary>
///     Catch-all class for any unparsable <see cref="Token"/>s
/// </summary>
internal class Unknown : Statement
{
    public ReadOnlyMemory<Token> Tokens { get; init; }

    public static new Unknown Parse(ref Parser current)
    {
        Parser parser = current;

        while (parser.Token is not Sentinel and not Terminal and not Separator and not Close)
        {
            parser.Advance();
        }

        if (ReferenceEquals(current.Token, parser.Token)) return null;

        return new Unknown { Tokens = current.AdvanceTo(parser) };
    }
}