// Copyright © 2023 Eric Budai

using OneOf;
using Ronin.Compiler;
using System.Collections;
using System.Collections.Generic;

namespace Ronin.Grammar;

/// <summary>
///     Represents a named indirection to a <see cref="Datum"/>, <see cref="Function.Declaration"/>, <see cref="Type.Declaration"/> or <see cref="Value"/>
/// </summary>
internal class Reference : IEnumerable<Reference.Component>
{
    public List<Component> Components { get; init; }

    public static Reference Parse(ref Parser current)
    {
        Parser parser = current;

        var components = parser.ParseRepeating<Component>();
        if (components.Count is 0) return null;
        current = parser;
        return new Reference { Components = components };
    }

    public IEnumerator<Component> GetEnumerator() => Components.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Components.GetEnumerator();

    public class Component : OneOfBase<Name, Value.Temporary>, IAggregable<Component>
    {
        protected Component(OneOf<Name, Value.Temporary> _) : base(_) { }

        public static implicit operator Component(Name name) => new(name);
        public static implicit operator Component(Value.Temporary value) => new(value);

        public static Component Parse(ref Parser current)
            => Name.Parse(ref current) is Name name
                ? name
                : Grammar.Value.Temporary.Parse(ref current);
    }
}