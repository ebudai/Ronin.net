// Copyright © 2023 Eric Budai

using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar.Aggregates;

/// <summary>
///     Comma-separated list of values in square brackets
/// </summary>
/// 
/// <remarks>
///     This is used to declare a List or a Lookup, or to retrieve an element from one
/// </remarks>
/// 
/// <example>
///     var apples => Apple[];
///     var apple = apples[7];
///     var capital cities => City[text];
///     const Ottawa => City;
///     capital cities["Canada"] = Ottawa;
/// </example>
/// 
internal class Index : Aggregate<Index, OpenSquareBracket, Value, Separator, CloseSquareBracket>
{
    
}
