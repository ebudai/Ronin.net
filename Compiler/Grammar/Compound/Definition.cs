// Copyright © 2023 Eric Budai

using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar.Compound;

/// <summary>
///     Body of <see cref="Scope"/>s, <see cref="Datatype.Declaration"/>s and <see cref="Function.Declaration"/>s.
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
    public Definition Parent { get; set; }
    public Dictionary<Identifier.Component, Datatype> Datatypes { get; } = new();
    public Dictionary<Identifier.Component, Datum> Data { get; } = new();
    public Dictionary<Identifier.Component, Function> Functions { get; } = new();    
    public Dictionary<Identifier.Component, Scope> Children { get; } = new();
}
