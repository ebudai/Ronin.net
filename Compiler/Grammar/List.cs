// Copyright © 2023 Eric Budai

using Ronin.Lexicon;

namespace Ronin.Grammar;

/// <summary>
///     List of <see cref="AnonymousValue"/>s specified directly in code
/// </summary>
/// 
/// <remarks>
///     <see cref="Separator"/>-delimited list of <see cref="AnonymousValue"/>s between <see cref="StartScope"/> and <see cref="EndScope"/>
/// </remarks>
/// 
/// <example>
///     var x = { 1, 2, seven, three };
///             ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
/// </example>
internal class List : Aggregate<List, StartScope, Value, Separator, EndScope>
{

}