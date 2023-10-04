// Copyright © 2023 Eric Budai

using Ronin.Lexicon;

namespace Ronin.Grammar;

/// <summary>
///     Aggregate of <see cref="Temporary"/>s intended for selecting an element from, or delcaring, a List or Lookup
/// </summary>
/// 
/// <remarks>
///     <see cref="Separator"/>-separated <see cref="Temporary"/>s between <see cref="OpenSquareBracket"/> and <see cref="CloseSquareBracket"/>
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
internal class Indexer : Aggregate<Indexer, OpenSquareBracket, Value, Separator, CloseSquareBracket>
{

}
