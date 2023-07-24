// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

/// <summary>
///     A <see cref="Constant"/>, <see cref="Compiled"/> value written directly in code
/// </summary>
/// 
/// <example>
///     var when = 12:33p;
///                ↑↑↑↑↑↑
///     constant cash = $75;
///                     ↑↑↑
///     let x = 7,000,876 + cash amount;
///             ↑↑↑↑↑↑↑↑↑
/// </example>
internal class Inline : AnonymousValue, IParsableSyntax<Inline>
{
    public new static Inline Parse(ref Parser current)
    {
        Parser parser = current;

        while (parser.IsNotFinished)
        {            
            if (parser.Token is not Lexicon.Literal) break;
            parser.Advance();
        }

        if (parser.Token == current.Token) return null;

        return new Inline { Source = parser.Commit(ref current) };
    }
}