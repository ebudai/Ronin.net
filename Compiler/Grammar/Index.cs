// Copyright © 2023 Eric Budai

using Ronin.Lexicon;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Grammar;

/// <summary>
///     Aggregate of <see cref="Value.Temporary"/>s intended for selecting an element from, or delcaring, a List or Lookup
/// </summary>
/// 
/// <remarks>
///     <see cref="Separator"/>-separated <see cref="Value.Temporary"/>s between <see cref="OpenSquareBracket"/> and <see cref="CloseSquareBracket"/>
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
internal class Index : Aggregate<Index, Open.SquareBracket, Value, Separator, Close.SquareBracket>
{
    
}
