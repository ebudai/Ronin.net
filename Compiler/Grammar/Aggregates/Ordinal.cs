// Copyright © 2023 Eric Budai

using Ronin.Lexicon;

namespace Ronin.Grammar.Aggregates;

/// <summary>
///     Aggregate of <see cref="Temporary"/>s intended for selecting an element from, or delcaring, a List or Lookup
/// </summary>
/// 
/// <remarks>
///     <see cref="SeparatorSymbol"/>-separated <see cref="Temporary"/>s between <see cref="OpenSquareBracketSymbol"/> and <see cref="CloseSquareBracketSymbol"/>
/// </remarks>
/// 
/// <example>
///     var apples => Apple[];
///                        ↑↑
///     var apple = apples[7];
///                       ↑↑↑
///     var capital cities => City[text];
///                               ↑↑↑↑↑↑
///     const Ottawa => City;
///     capital cities["Canada"] = Ottawa;
///                   ↑↑↑↑↑↑↑↑↑↑
///     var multi-dimensional list => number[7,15,87];
///                                         ↑↑↑↑↑↑↑↑↑
///     var selected value = multi-dimensional list[3, 1, 0];
///                                                ↑↑↑↑↑↑↑↑↑
/// </example>
internal class Ordinal : AggregateSyntax<Ordinal, OpenSquareBracketSymbol, Value, SeparatorSymbol, CloseSquareBracketSymbol>
{
    
}
