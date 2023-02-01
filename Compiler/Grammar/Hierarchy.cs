// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;

namespace Ronin.Grammar;

/// <summary>
///     Names a <see cref="Scope"/> via 'part of', or exposes one to the current <see cref="Scope"/> via 'import'
/// </summary>
/// 
/// <example>
///     part of best package for weather lookups
///     
///     import best package for weather lookups
///     import git://github.com/ebudai/Ronin as ronin
/// </example>
/// 
internal class Hierarchy : Syntax, Compiler.IParsable<Hierarchy>
{
    public Keyword Direction { get; init; }
    public List<Component> Components { get; init; }

    public static Hierarchy Parse(ref Parser context)
    {
        var direction = context.Current is PartOf or Import ? context.Current as Keyword : null;
        if (direction is null) return null;

        Parser parser = context;
        parser.Advance();

        var components = parser.ParseRepeating<Component>();
        if (components.Count is 0) return null;

        return new Hierarchy 
        {
            Direction = direction,
            Components = components,
            Source = parser.Commit(ref context) 
        };
    }

    public class Component : Syntax, Compiler.IParsable<Component>
    {
        public static Component Parse(ref Parser context)
        {
            Parser parser = context;

            var syntax = Name.Parse(ref parser) ?? Scalar.Parse(ref parser) as Syntax;

            if (syntax is null) return null;

            return new Component { value = syntax, Source = parser.Commit(ref context) };
        }
        
        public static implicit operator Name(Component component) => component.value as Name;
        public static implicit operator Scalar(Component component) => component.value as Scalar;

        private Syntax value;
    }
}