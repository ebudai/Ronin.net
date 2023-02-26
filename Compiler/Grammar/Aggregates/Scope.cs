// Copyright © 2023 Eric Budai

using Ronin.Lexicon;

namespace Ronin.Grammar.Aggregates;

/// <summary>
///     Body of conditionals, loops, <see cref="DatatypeDeclarationSyntax"/>s and <see cref="FunctionDeclarationSyntax"/>s.  Can also be a <see cref="Temporary"/>.
/// </summary>
/// 
/// <remarks>
///     <see cref="TerminalSymbol"/>-separated <see cref="Statement"/>s between <see cref="OpenBraceSymbol"/> and <see cref="CloseBraceSymbol"/>
/// </remarks>
/// 
/// <example>
///     datatype Speaker
///     → {
///     →     var volume => number; 
///     →     var base => number; 
///     →     var treble => number; 
///     →     var brand => text;
///     → }
///       ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
/// </example>
internal class Scope : AggregateSyntax<Scope, OpenBraceSymbol, Statement, TerminalSymbol, CloseBraceSymbol>
{

}
