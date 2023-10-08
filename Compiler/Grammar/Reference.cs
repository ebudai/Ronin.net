// Copyright © 2023 Eric Budai

using OneOf;
using Ronin.Compiler;
using Ronin.Lexicon;
using System.Collections;
using System.Collections.Generic;
using static Ronin.Grammar.Value;

namespace Ronin.Grammar;

/// <summary>
///     Represents a named indirection to a <see cref="Datum"/>, <see cref="Function"/>, <see cref="Type"/> or <see cref="Value"/>
/// </summary>
internal class Reference : IEnumerable<Reference.Component>
{
    public List<Component> Components { get; init; }

    public static Reference Parse(ref Parser current)
    {
        Parser parser = current;

        if (current.Token is Keyword) return null;

        var components = parser.ParseRepeating<Component>();
        if (components.Count is 0) return null;
        if (components.Count is 1 
            && components[0].IsT1 
            && components[0].AsT1 is Literal) return null;
        current = parser;
        return new Reference { Components = components };
    }

    public IEnumerator<Component> GetEnumerator() => Components.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Components.GetEnumerator();

    public class Component : OneOfBase<Name, Temporary>, IParsable<Component>
    {
        protected Component(OneOf<Name, Temporary> _) : base(_) { }

        public static implicit operator Component(Name name) => new(name);
        public static implicit operator Component(Temporary value) => new(value);

        public static Component Parse(ref Parser current)
        {
            if (Name.Parse(ref current) is Name name) return name;
            if (Temporary.Parse(ref current) is Temporary value) return value;
            return null;
        }
    }
}