// Copyright © 2023 Eric Budai

using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar.Aggregates;

/// <summary>
///     Aggregate of <see cref="Value"/>s intended for setting <see cref="Parameters"/>
/// </summary>
/// 
/// <remarks>
///     <see cref="Separator"/>-separated <see cref="Value"/>s between <see cref="OpenParenthesis"/> and <see cref="CloseParenthesis"/>
/// </remarks>
/// 
/// <example>
///     var x = pack(a, 8.2, "first name");
///                 ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
/// </example>
internal class Arguments : AggregateSyntax<Arguments, OpenParenthesis, Value, Separator, CloseParenthesis>
{
    
}