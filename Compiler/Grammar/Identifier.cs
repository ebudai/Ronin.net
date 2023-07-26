// Copyright © 2023 Eric Budai

using Ronin.Compiler;

namespace Ronin.Grammar;

/// <summary>
///     A unique name for a <see cref="Datatype.Declaration"/> or a <see cref="Function.Declaration"/>
///     which can contain multiple <see cref="Name"/>s and <see cref="Parameters"/>
/// </summary>
internal class Identifier : Syntax, IParsableSyntax<Identifier>
{
    public List<Component> Components { get; init; } = new();

    public static implicit operator Identifier(Name name) => new() { Components = new() { new Component { value = name } } };

    public static Identifier Parse(ref Parser current)
    {
        Parser parser = current;

        var components = parser.ParseRepeating<Component>();
        if (components.Count is 0) return null;

        return new Identifier { Components = components, Source = parser.Commit(ref current) };
    }

    public class Component : CompositeSyntax<Component, Name, Parameters> { }
}