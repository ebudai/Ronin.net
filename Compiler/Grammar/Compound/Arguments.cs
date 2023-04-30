// Copyright © 2023 Eric Budai

using Ronin.Lexicon.Punctuation;

namespace Ronin.Grammar.Compound;

/// <summary>
///     Aggregate of <see cref="Anonymous"/>s intended for setting <see cref="Parameters"/>
/// </summary>
/// 
/// <remarks>
///     <see cref="Separator"/>-separated <see cref="Anonymous"/>s between <see cref="OpenParenthesis"/> and <see cref="CloseParenthesis"/>
/// </remarks>
/// 
/// <example>
///     var x = pack(a, 8.2, "first name");
///                 ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
/// </example>
internal class Arguments : Aggregate<Arguments, OpenParenthesis, Value, Separator, CloseParenthesis>
{
    
}