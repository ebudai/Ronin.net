// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Aggregates;

namespace Ronin.Grammar;

internal class Reference : Syntax, Compiler.IParsable<Reference>
{
    public List<Component> Components { get; init; }
    public Ordinal Ordinal { get; init; }

    public static Reference Parse(ref Parser context)
    {
        Parser parser = context;

        var components = parser.ParseRepeating<Component>();
        if (components.Count is 0) return null;
        if (components.All(component => component.IsNot<Name>())) return null;
 
        var ordinal = Ordinal.Parse(ref parser);

        return new Reference
        {
            Components = components,
            Ordinal = ordinal,
            Source = parser.Commit(ref context)
        };
    }

    public class Component : CompositeSyntax<Component, Name, Scalar, Arguments> { }
}