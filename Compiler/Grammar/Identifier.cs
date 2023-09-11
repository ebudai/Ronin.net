// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Ronin.Grammar;

/// <summary>
///     A unique name for a <see cref="Datatype.Declaration"/>, <see cref="Datum.Declaration"/> or a <see cref="Function.Declaration"/>
///     which can contain multiple <see cref="Identifier"/>s and <see cref="Parameters"/>
/// </summary>
internal class Identifier : Syntax, IParsableSyntax<Identifier>, IEnumerable<Identifier.Component>
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

    public override bool Equals(object obj) => Components.SequenceEqual(obj as Identifier);

    public override int GetHashCode()
    {
        HashCode hashcode = new();
        foreach (var component in Components) hashcode.Add(component);
        return hashcode.ToHashCode();
    }

    public IEnumerator<Component> GetEnumerator() => Components.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Components.GetEnumerator();

    public class Component : CompositeSyntax<Component, Name, Parameters>
    {
        public override bool Equals(object obj)
        {
            if (obj is Component) return base.Equals(obj);

            if (obj is not Reference.Component reference) return false;
            
            if (value is Name) return reference.Equals(value);

            Value.Anonymous anonymous = reference;
            var inputcount = anonymous is Inputs inputs ? inputs.Count : 1;

            var parameters = value as Parameters;
            return inputcount >= parameters.MandatoryInputsCount() && inputcount <= parameters.Data.Count;
        }

        public override int GetHashCode() => base.GetHashCode();

        //public static implicit operator Component(Name name) => new() { value = name, Source = name.Source };
        //public static implicit operator Component(Parameters parameters) => new() { value = parameters, Source = parameters.Source };
    }
}