// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Language;
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
    public Dictionary<Identifier.Component, Datatype> Datatypes { get; init; } = new();
    public Dictionary<Identifier.Component, Datum> Data { get; init; } = new();
    public Dictionary<Identifier.Component, Function> Functions { get; init; } = new();
    public List<Module> Imports { get; init; } = new();

    public List<Error> Add(Identifier identifier, Function function) { return null; }
    public List<Error> Add(Identifier identifier, Datatype datatype) { return null; }
    public List<Error> Add(Name name, Datum datum) { return null; }
    
    public object Find(Identifier identifier) { return null; }
}
