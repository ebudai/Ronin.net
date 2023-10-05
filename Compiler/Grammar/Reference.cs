// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using System.Collections;
using System.Collections.Generic;

namespace Ronin.Grammar;

/// <summary>
///     Represents a named indirection to a <see cref="Datum"/>, <see cref="Function.Declaration"/>, <see cref="Type.Declaration"/> or <see cref="Value"/>
/// </summary>

using Component = Grammar<Name, Value.Temporary>;

internal class Reference : IGrammar<Reference>, IEnumerable<Component>
{
    public List<Component> Components { get; init; }

    public static Reference Parse(ref Parser current)
    {
        Parser parser = current;

        var components = parser.ParseRepeating<Component>();
        return components.Count is 0
            ? null
            : new Reference { Components = components };
    }

    public IEnumerator<Component> GetEnumerator() => Components.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Components.GetEnumerator();
}