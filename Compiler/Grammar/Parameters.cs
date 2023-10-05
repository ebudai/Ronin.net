// Copyright © 2023 Eric Budai

using Ronin.Lexicon;

namespace Ronin.Grammar;

/// <summary>
///     Aggregate of <see cref="Datum.Declaration"/> used to declare variables to enter into a <see cref="Function.Declaration"/>
/// </summary>
/// 
/// <remarks>
///     <see cref="Separator"/>-separated <see cref="Datum"/>s between <see cref="OpenParenthesis"/> and <see cref="CloseParenthesis"/>
/// </remarks>
/// 
/// <example>
///     function thing (x => number, y => money) with stuff { return 8; }
///                    ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
/// </example>
using Parameter = Grammar<Datum, Association>;

internal class Parameters : Aggregate<Parameters, OpenParenthesis, Parameter, Separator, CloseParenthesis>
{

}