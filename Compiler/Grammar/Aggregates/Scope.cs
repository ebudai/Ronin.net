// Copyright © 2023 Eric Budai

using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar.Aggregates;

/// <summary>
///     Semicolon-separated list of <see cref="Statement"/> in braces
/// </summary>
/// 
/// <remarks>
///     Used as the body of conditionals, loops, <see cref="Function"/> and <see cref="Datatype"/>
/// </remarks>
/// 
/// <example>
///     datatype Speaker
///     {
///         var volume => number; 
///         var base => number; 
///         var treble => number; 
///         var brand => text;
///     }
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
