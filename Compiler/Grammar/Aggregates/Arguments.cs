// Copyright © 2023 Eric Budai

using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar.Aggregates;

/// <summary>
///     Comma-separated list of values in parenthesis
/// </summary>
/// 
/// <remarks>
///     This is used to supply values to <see cref="Parameters"/>
/// </remarks>
/// 
/// <example>
///     var x = pack(a, 8.2, "first name");
/// </example>
internal class Arguments : Aggregate<Arguments, OpenParenthesis, Value, Separator, CloseParenthesis>
{
    
}