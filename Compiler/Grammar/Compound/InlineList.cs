// Copyright © 2023 Eric Budai

using Ronin.Lexicon.Punctuation;

namespace Ronin.Grammar.Compound;

/// <summary>
///     List of <see cref="Value"/>s specified directly in code
/// </summary>
/// 
/// <remarks>
///     <see cref="Separator"/>-delimited list of <see cref="Value"/>s between <see cref="OpenBrace"/> and <see cref="CloseBrace"/>
/// </remarks>
/// 
/// <example>
///     var x = { 1, 2, seven, three };
///             ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
/// </example>
internal class InlineList : Aggregate<InlineList, OpenBrace, Value, Separator, CloseBrace>
{

}