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

        var ordinal = Ordinal.Parse(ref parser);

        return new Reference
        {
            Components = components,
            Ordinal = ordinal,
            Source = parser.Commit(ref context)
        };
    }

    public class Component : Syntax, Compiler.IParsable<Component>
    {
        public static Component Parse(ref Parser context)
        {
            Parser parser = context;

            var syntax = Name.Parse(ref parser)
                ?? Scalar.Parse(ref parser)
                ?? Arguments.Parse(ref parser) as Syntax;

            if (syntax is null) return null;

            return new Component { value = syntax, Source = parser.Commit(ref context) };
        }

        public static implicit operator Name(Component component) => component.value as Name;
        public static implicit operator Scalar(Component component) => component.value as Scalar;
        public static implicit operator Arguments(Component component) => component.value as Arguments;

        private Syntax value;
    }
}