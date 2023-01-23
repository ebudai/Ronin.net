// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Aggregates;

namespace Ronin.Grammar;

internal class Identifier : Syntax, Compiler.IParsable<Identifier>
{
    public List<Component> Components { get; init; }

    public static Identifier Parse(ref Parser context)
    {
        Parser parser = context;

        var components = parser.ParseRepeating<Component>();
        if (components.Count is 0) return null;

        return new Identifier { Components = components, Source = parser.Commit(ref context) };
    }

    /// <summary>
    ///     Union of <see cref="Name"/> and <see cref="Parameters"/>
    /// </summary>
    public class Component : Syntax, Compiler.IParsable<Component>
    {
        public static Component Parse(ref Parser context)
        {
            Parser parser = context;
            
            var value = Name.Parse(ref parser) ?? Parameters.Parse(ref parser) as Syntax;
            if (value is null) return null;

            return new Component { value = value, Source = parser.Commit(ref context) };
        }

        public static implicit operator Name(Component component) => component.value as Name;
        public static implicit operator Parameters(Component component) => component.value as Parameters;

        private Syntax value;
    }
}
