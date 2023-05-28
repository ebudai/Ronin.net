// Copyright © 2023 Eric Budai

using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar.Compound;

/// <summary>
///     Aggregate of <see cref="Parameter"/> used to declare variables to enter into a <see cref="FunctionDeclaration"/>
/// </summary>
/// 
/// <remarks>
///     <see cref="Separator"/>-separated <see cref="DatumDeclaration"/>s between <see cref="StartValues"/> and <see cref="EndValues"/>
/// </remarks>
/// 
/// <example>
///     function thing (x => number, y => money) with stuff { return 8; }
///                    ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
/// </example>
internal class Parameters : Aggregate<Parameters, StartValues, DatumDeclaration, Separator, EndValues> 
{
    
}
