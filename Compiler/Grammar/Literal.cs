// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon.Keywords;

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
internal class Literal : Anonymous, IParsableSyntax<Literal>
{
    public new static Literal Parse(ref Parser current)
    {
        Parser parser = current;

        while (parser.IsNotFinished)
        {            
            if (parser.Token is not Lexicon.Literal) break;
            parser.Advance();
        }

        if (parser.Token == current.Token) return null;

        return new Literal { Source = parser.Commit(ref current) };
    }
}