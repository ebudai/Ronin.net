// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Ronin.Grammar;

/// <summary>
///     A unique name for a <see cref="Type"/>, <see cref="Datum"/> or a <see cref="Function"/>
///     which can contain multiple <see cref="Name"/>s and <see cref="Parameters"/>
/// </summary>
internal class Identifier : IEnumerable<Identifier.Component>
{
    public ReadOnlySpan<Component> Span => CollectionsMarshal.AsSpan(Components);

    private List<Component> Components { get; init; } = new();

    public static Identifier Parse(ref Parser current)
    {
        Parser parser = current;

        var components = parser.ParseRepeating<Component>();
        if (components.Count is 0) return null;
        current = parser;
        return new Identifier { components };
    }

    public Component this[int i] => Components[i];

    public void Add(Name name) => Components.Add(name);
    public void Add(IEnumerable<Component> components) => Components.AddRange(components);

    public int Count => Components.Count;

    public void ResolveTypes(Scope context)
    {
        foreach (var component in this)
        {
            component.AsParameters?.ResolveTypes(context);
        }
    }

    public void ResolveFunctions(Scope context)
    {
        foreach (var component in this)
        {
            component.AsParameters?.ResolveFunctions(context);
        }
    }

    public void ResolveData(Scope context)
    {
        foreach (var component in this)
        {
            component.AsParameters?.ResolveData(context);
        }
    }

    public IEnumerator<Component> GetEnumerator() => Components.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Components.GetEnumerator();

    public class Component : Compiler.IParsable<Component>
    {
        private Component(Name name) => value = name;
        private Component(Parameters parameters) => value = parameters;

        public static implicit operator Component(Name name) => new(name);
        public static implicit operator Component(Parameters parameters) => new(parameters);

        public static Component Parse(ref Parser current)// => Name.Parse(ref current) is Name name ? name : Parameters.Parse(ref current);
        {
            if (Name.Parse(ref current) is Name name) return name;
            if (Parameters.Parse(ref current) is Parameters parameters) return parameters;
            return null;
        }

        public Name AsName => value as Name;
        public Parameters AsParameters => value as Parameters;

        private readonly object value;
    }
}