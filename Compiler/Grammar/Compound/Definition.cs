// Copyright © 2023 Eric Budai

using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar.Compound;

/// <summary>
///     Body of conditionals, loops, <see cref="DatatypeDeclaration"/>s and <see cref="FunctionDeclaration"/>s.
/// </summary>
/// 
/// <remarks>
///     <see cref="Terminal"/>-separated <see cref="Statement"/>s between <see cref="StartScope"/> and <see cref="EndScope"/>
/// </remarks>
/// 
/// <example>
///     datatype Speaker
///   → {
///   →     var volume => number; 
///   →     var base => number; 
///   →     var treble => number; 
///   →     var brand => text;
///   → }
///     ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
/// </example>
internal class Definition : Aggregate<Definition, StartScope, Statement, Terminal, EndScope>
{
    public Dictionary<Identifier, Datatype> Datatypes { get; } = new();
    public Dictionary<Identifier, Datum> Data { get; } = new();
    public Dictionary<Identifier, Function> Functions { get; } = new();
    public List<Statement> Instructions { get; } = new();
}
