// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Language;
using Ronin.Lexicon.Symbols;
using System.Runtime.InteropServices;

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
    public Dictionary<Identifier.Component, Definition> Children { get; } = new();

    public List<Error> Add(Function.Declaration declaration, Function function)
    {
        var components = CollectionsMarshal.AsSpan(declaration.Identifier.Components);

        if (Find(components) is not null) return Error.Redefinition(declaration);

        return null; 
    }

    public List<Error> Add(Identifier identifier, Datatype datatype) { return null; }
    public List<Error> Add(Name name, Datum datum) { return null; }
    
    public List<object> Find(Identifier identifier) { return null; }
    public List<object> Find(Reference reference) { return null; }

    private List<Error> Add(ReadOnlySpan<Identifier.Component> components, Function function)
    {
        return null;
    }

    private List<object> Find(ReadOnlySpan<Identifier.Component> identifier) 
    {
        List<object> found = new();
        if (identifier.IsEmpty) return found;
        found.AddRange(Parent.Find(identifier));
        if (Datatypes.TryGetValue(identifier[0], out var datatype)) found.Add(datatype);
        if (Data.TryGetValue(identifier[0], out var datum)) found.Add(datum);
        if (Data.TryGetValue(identifier[0], out var function)) found.Add(function);
        if (Children.TryGetValue(identifier[0], out var definition)) found.AddRange(definition.Find(identifier[1..]));
        return found;
    }
}
