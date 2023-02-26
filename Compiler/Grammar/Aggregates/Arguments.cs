// Copyright © 2023 Eric Budai

using Ronin.Lexicon;

namespace Ronin.Grammar.Aggregates;

/// <summary>
///     Aggregate of <see cref="Value"/>s intended for setting <see cref="Parameters"/>
/// </summary>
/// 
/// <remarks>
///     <see cref="SeparatorSymbol"/>-separated <see cref="Value"/>s between <see cref="OpenParenthesisSymbol"/> and <see cref="CloseParenthesisSymbol"/>
/// </remarks>
/// 
/// <example>
///     var x = pack(a, 8.2, "first name");
///                 ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
/// </example>
internal class Arguments : AggregateSyntax<Arguments, OpenParenthesisSymbol, Value, SeparatorSymbol, CloseParenthesisSymbol>
{
    
}