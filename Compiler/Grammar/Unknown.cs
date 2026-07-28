// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System;

namespace Ronin.Grammar;

/// <summary>
///     Catch-all class for any unparsable <see cref="Token"/>s
/// </summary>
internal class Unknown : Statement, IError
{
    /// <summary>
    ///     Recovery is not acceptance. This held its tokens and implemented
    ///     nothing, so «+;» was one statement and zero problems and exit zero —
    ///     the catch-all that keeps the parser moving was also the one that made
    ///     any unrecognisable input legal.
    /// </summary>
    public string Reason { get; } = "unknown syntax";

    public ReadOnlyMemory<Token> Tokens { get; init; }

    public static new Unknown Parse(ref Parser current)
    {
        var tokens = Parser.Recover(ref current, current);

        // consuming nothing is not an unknown statement, it is no statement —
        // and returning one anyway is what would stall the loop above it
        return tokens.Length is 0 ? null : new Unknown { Tokens = tokens };
    }
}
