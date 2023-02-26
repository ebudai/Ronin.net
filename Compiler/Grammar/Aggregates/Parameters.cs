// Copyright © 2023 Eric Budai

using Ronin.Lexicon;

namespace Ronin.Grammar.Aggregates;

/// <summary>
///     Aggregate of <see cref="Parameter"/> used to declare variables to enter into a <see cref="FunctionDeclarationSyntax"/>
/// </summary>
/// 
/// <remarks>
///     <see cref="SeparatorSymbol"/>-separated <see cref="Parameter"/>s between <see cref="OpenParenthesisSymbol"/> and <see cref="CloseParenthesisSymbol"/>
/// </remarks>
/// 
/// <example>
///     function thing (x => number, y => money) with stuff { return 8; }
///                    ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
/// </example>
internal class Parameters : AggregateSyntax<Parameters, OpenParenthesisSymbol, DatumDeclarationSyntax, SeparatorSymbol, CloseParenthesisSymbol> 
{
    
}
