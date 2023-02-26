// Copyright © 2023 Eric Budai

using Ronin.Lexicon;

namespace Ronin.Grammar.Aggregates;

/// <summary>
///     List of <see cref="Value"/>s specified directly in code
/// </summary>
/// 
/// <remarks>
///     <see cref="SeparatorSymbol"/>-delimited list of <see cref="Value"/>s between <see cref="OpenBraceSymbol"/> and <see cref="CloseBraceSymbol"/>
/// </remarks>
/// 
/// <example>
///     var x = { 1, 2, seven, three };
///             ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
/// </example>
internal class InlineListSyntax : AggregateSyntax<InlineListSyntax, OpenBraceSymbol, Value, SeparatorSymbol, CloseBraceSymbol>
{

}