// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Grammar;

/// <summary>
///     Represents all values between a lowest and highest value
/// </summary>
/// 
/// <example>
///     var range = 3..a;
///                 ↑↑↑↑
///     var another range = 2..7;
///                         ↑↑↑↑
///     var last range = low..high;
///                      ↑↑↑↑↑↑↑↑↑
/// </example>
internal class Interval : Syntax, IParsableSyntax<Interval>
{
    public static Interval Parse(ref Parser current)
    {
        Parser parser = current;

        if (parser.TryAdvance<Lexicon.Symbols.Range>() is false) return null;

        return new Interval { Source = parser.Commit(ref current) };
    }
}
