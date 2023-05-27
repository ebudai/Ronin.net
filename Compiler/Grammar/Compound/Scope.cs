// Copyright © 2023 Eric Budai

using Ronin.Lexicon.Punctuation;

namespace Ronin.Grammar.Compound;

/// <summary>
///     Body of conditionals, loops, <see cref="DatatypeDeclaration"/>s and <see cref="FunctionDeclaration"/>s.  Can also be a <see cref="Temporary"/>.
/// </summary>
/// 
/// <remarks>
///     <see cref="Terminal"/>-separated <see cref="Statement"/>s between <see cref="OpenBrace"/> and <see cref="CloseBrace"/>
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
internal class Scope : Aggregate<Scope, OpenBrace, Statement, Terminal, CloseBrace>
{

}
