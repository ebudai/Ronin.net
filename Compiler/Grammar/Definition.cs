// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Language;
using Ronin.Lexicon.Symbols;
using System.Runtime.InteropServices;
using System.Transactions;

namespace Ronin.Grammar;

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
    public Dictionary<Identifier.Component, Function> Functions { get; init; } = new();
    public Dictionary<Identifier.Component, Datum> Data { get; init; } = new();    
    public List<Module> Imports { get; init; } = new();
    public Dictionary<Identifier.Component, Definition> Children { get; } = new();

    public List<Error> Add(Function.Declaration declaration, Function function)
    {
        var components = CollectionsMarshal.AsSpan(declaration.Identifier.Components);
        return Add(components, function);
    }

    public List<Error> Add(Datatype.Declaration declaration, Datatype datatype)
    {
        var components = CollectionsMarshal.AsSpan(declaration.Identifier.Components);
        return Add(components, datatype);
    }

    public List<Error> Add(Datum.Declaration declaration, Datum datum) 
    {
        Identifier identifier = declaration.Name;
        var components = CollectionsMarshal.AsSpan(identifier.Components);
        return Add(components, datum);
    }

    // finds an existing identifier in the same or parent scope
    public Identifier Existing(Identifier identifier)
    {
        Identifier existing = new();
        var components = Find(CollectionsMarshal.AsSpan(identifier.Components));
        existing?.Components.AddRange(components);
        return existing;
    }

    public List<object> Find(Reference reference)
    {
        throw new NotImplementedException();
    }

    private List<Error> Add(ReadOnlySpan<Identifier.Component> components, Function function)
    {
        if (components.Length is 1)
        {
            if (Functions.TryAdd(components[0], function)) return Error.None;
        }

        throw new NotImplementedException();
    }

    private List<Error> Add(ReadOnlySpan<Identifier.Component> components, Datatype datatype)
    {
        throw new NotImplementedException();
    }

    private List<Error> Add(ReadOnlySpan<Identifier.Component> components, Datum datum)
    {
        throw new NotImplementedException();
    }

    private List<Identifier.Component> Find(ReadOnlySpan<Identifier.Component> identifier)
    {
        if (identifier.IsEmpty) return new();
        
        if (identifier.Length is 1)
        {
            foreach (var component in Datatypes.Keys)
            {
                if (component.Equals(identifier[0])) return new() { component };
            }

            foreach (var component in Functions.Keys)
            {
                if (component.Equals(identifier[0])) return new() { component };
            }

            foreach (var component in Data.Keys)
            {
                if (component.Equals(identifier[0])) return new() { component };
            }

            return new();
        }

        if (Children.TryGetValue(identifier[0], out var child))
        {
            List<Identifier.Component> components = new(identifier.Length) { Children.GetKey(identifier[0]) };
            components.AddRange(child.Find(identifier[1..]));
        }

        return new();
    }
}
