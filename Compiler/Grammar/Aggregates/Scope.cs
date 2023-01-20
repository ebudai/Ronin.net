// Copyright © 2023 Eric Budai

using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar.Aggregates;

/// <summary>
///     Body of conditionals, loops, <see cref="Function"/>s and <see cref="Datatype"/>s.  Can also be a <see cref="Value"/>.
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
    /*public Scope Parent { get; init; }
    
    public List<Datum> Data { get; } = new();
    public List<Function> Functions { get; } = new();
    public List<Datatype> Datatypes { get; } = new();

    //public override string ToString() => '{' + string.Join(",", Values) + '}';

    public static Scope Global;*/
}
