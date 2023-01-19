// Copyright © 2023 Eric Budai

using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar.Aggregates;

/// <summary>
///     Comma-separated list of <see cref="Parameter"/> in parenthesis
/// </summary>
/// 
/// <remarks>
///     Used to declare one or more values as inputs for a function call
/// </remarks>
/// 
/// <example>
///     function thing (x => number, y => money) with stuff { return 8; }
/// </example>
internal class Parameters : Aggregate<Parameters, OpenParenthesis, Parameter, Separator, CloseParenthesis> 
{
    
}
