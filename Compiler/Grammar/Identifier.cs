// Copyright © 2023 Eric Budai

using OneOf;
using Ronin.Compiler;
using System.Collections;
using System.Collections.Generic;

namespace Ronin.Grammar;

/// <summary>
///     A unique name for a <see cref="Type.Declaration"/>, <see cref="Datum.Declaration"/> or a <see cref="Function.Declaration"/>
///     which can contain multiple <see cref="Identifier"/>s and <see cref="Parameters"/>
/// </summary>
internal class Identifier : IGrammar<Identifier>, IEnumerable<Identifier.Component>
{
    public List<Component> Components { get; init; } = new();

    public static Identifier Parse(ref Parser current)
    {
        Parser parser = current;

        var components = parser.ParseRepeating<Component>();
        return components.Count is 0
            ? null
            : new Identifier { Components = components };
    }

    public IEnumerator<Component> GetEnumerator() => Components.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Components.GetEnumerator();

    public class Component : OneOfBase<Name, Parameters>, IGrammar<Component>
    {
        protected Component(OneOf<Name, Parameters> _) : base(_) { }

        public static implicit operator Component(Name name) => name;
        public static implicit operator Component(Parameters parameters) => parameters;

        public static Component Parse(ref Parser current)
            => Name.Parse(ref current) is Name name
                ? name
                : Parameters.Parse(ref current);
    }
}