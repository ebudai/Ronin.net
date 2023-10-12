// Copyright © 2023 Eric Budai

using OneOf;
using Ronin.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Grammar;

/// <summary>
///     A unique name for a <see cref="Type"/>, <see cref="Datum"/> or a <see cref="Function"/>
///     which can contain multiple <see cref="Name"/>s and <see cref="Parameters"/>
/// </summary>
internal class Identifier : IEnumerable<Identifier.Component>
{
    public List<Component> Components { get; init; } = new();

    public static Identifier Parse(ref Parser current)
    {
        Parser parser = current;

        var components = parser.ParseRepeating<Component>();
        if (components.Count is 0) return null;
        current = parser;
        return new Identifier { Components = components };
    }

    public void ResolveTypes(Scope context)
    {
        foreach (var component in Components)
        {
            if (component.IsT1 is false) continue;
            component.AsT1.ResolveTypes(context);
        }
    }

    public void ResolveFunctions(Scope context)
    {
        foreach (var component in Components)
        {
            if (component.IsT1 is false) continue;
            component.AsT1.ResolveFunctions(context);
        }
    }

    public IEnumerator<Component> GetEnumerator() => Components.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Components.GetEnumerator();

    public class Component : OneOfBase<Name, Parameters>, IParsable<Component>
    {
        protected Component(OneOf<Name, Parameters> _) : base(_) { }

        public static implicit operator Component(Name name) => new(name);
        public static implicit operator Component(Parameters parameters) => new(parameters);

        public static Component Parse(ref Parser current)
        {
            if (Name.Parse(ref current) is Name name) return name;
            if (Parameters.Parse(ref current) is Parameters parameters) return parameters;
            return null;
        }
    }
}