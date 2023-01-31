// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Reference : Syntax, Compiler.IParsable<Reference>
{
    public List<Component> Components { get; init; }
    public Ordinal Ordinal { get; init; }

    public static Reference Parse(ref Parser context)
    {
        if (context.Current is Keyword) return null;

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

        public bool IsNot<T>() => value is not T;

        private Syntax value;
    }
}