// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar;

/// <summary>
///     A unique name for a <see cref="Datatype.Declaration"/>, <see cref="Datum.Declaration"/> or a <see cref="Function.Declaration"/>
///     which can contain multiple <see cref="Identifier"/>s and <see cref="Parameters"/>
/// </summary>
internal class Identifier : Syntax, IParsableSyntax<Identifier>
{
    public List<Component> Components { get; init; } = new();

    public static Identifier Parse(ref Parser current)
    {
        Parser parser = current;

        var components = parser.ParseRepeating<Component>();
        if (components.Count is 0) return null;

        return new Identifier 
        { 
            Components = components, 
            Source = parser.Commit(ref current) 
        };
    }

    public class Component : CompositeSyntax<Component, Name, Parameters>
    {
        /*public override bool Equals(object obj)
        {
            if (obj is Component component)
            {
                return base.Equals(component);
            }

            if (obj is not Reference.Component reference) return false;
            
            if (value is Name)
            {
                Name name = reference;
                return name?.Equals(value) ?? false;
            }

            var parameters = value as Parameters;
            var mandatory = 0;
            foreach (var parameter in parameters.Values)
            {
                if (parameter.Modifiers.Is<Optional>()) continue;
                if (parameter.Initializer is not null) continue;
                ++mandatory;
            }

            AnonymousValue anonymous = reference;
            if (anonymous is null) return false;
            var inputcount = anonymous is Inputs inputs ? inputs.Values.Count : 1;
            return inputcount >= mandatory && inputcount <= parameters.Values.Count;
        }

        public override int GetHashCode() => base.GetHashCode();*/

        public static implicit operator Component(Name name) => new() { value = name };
        public static implicit operator Component(Parameters parameters) => new() { value = parameters };
    }
}