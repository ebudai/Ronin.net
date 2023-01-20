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
        public Syntax Syntax { get; init; }

        public static Component Parse(ref Parser context)
        {
            Parser parser = context;
            
            var syntax = Name.Parse(ref parser) ?? Parameters.Parse(ref parser) as Syntax;
            if (syntax is null) return null;

            return new Component { Syntax = syntax, Source = parser.Commit(ref context) };
        }        
    }
}
