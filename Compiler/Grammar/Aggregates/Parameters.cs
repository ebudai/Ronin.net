// Copyright © 2023 Eric Budai

using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar.Aggregates;

/// <summary>
///     Aggregate of <see cref="Parameter"/> used to declare variables to enter into a <see cref="FunctionDeclarationSyntax"/>
/// </summary>
/// 
/// <remarks>
///     <see cref="Separator"/>-separated <see cref="Parameter"/>s between <see cref="OpenParenthesis"/> and <see cref="CloseParenthesis"/>
/// </remarks>
/// 
/// <example>
///     function thing (x => number, y => money) with stuff { return 8; }
///                    ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
/// </example>
internal class Parameters : AggregateSyntax<Parameters, OpenParenthesis, DatumDeclarationSyntax, Separator, CloseParenthesis> 
{
    
}
