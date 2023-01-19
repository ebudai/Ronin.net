// Copyright © 2023 Eric Budai

using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar.Aggregates;

/// <summary>
///     Comma-separated list of values in square brackets
/// </summary>
/// 
/// <remarks>
///     This is used to declare a List or a Lookup, or to retrieve an element from one of those datatypes
/// </remarks>
/// 
/// <example>
///     var apples => Apple[];
///     var apple = apples[7];
///     var 
/// </example>
/// 
internal class Index : Aggregate<Index, OpenSquareBracket, Value, Separator, CloseSquareBracket>
{
    
}
