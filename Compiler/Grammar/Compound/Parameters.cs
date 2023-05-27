// Copyright © 2023 Eric Budai

using Ronin.Lexicon.Punctuation;

namespace Ronin.Grammar.Compound;

/// <summary>
///     Aggregate of <see cref="Parameter"/> used to declare variables to enter into a <see cref="FunctionDeclaration"/>
/// </summary>
/// 
/// <remarks>
///     <see cref="Separator"/>-separated <see cref="DatumDeclaration"/>s between <see cref="OpenParenthesis"/> and <see cref="CloseParenthesis"/>
/// </remarks>
/// 
/// <example>
///     function thing (x => number, y => money) with stuff { return 8; }
///                    ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
/// </example>
internal class Parameters : Aggregate<Parameters, OpenParenthesis, DatumDeclaration, Separator, CloseParenthesis> 
{
    
}
