// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Ronin.Grammar;

/// <summary>
///     A unique name for a <see cref="Type"/>, <see cref="Datum"/> or a <see cref="Function"/>
///     which can contain multiple <see cref="Name"/>s and <see cref="Parameters"/>
/// </summary>
internal class Identifier : IEnumerable<Identifier.Component>
{
    public ReadOnlySpan<Component> Span => CollectionsMarshal.AsSpan(Components);

    private List<Component> Components { get; init; } = new();

    public static Identifier Parse(ref Parser current)
    {
        Parser parser = current;

        var components = parser.ParseRepeating<Component>();
        if (components.Count is 0) return null;
        current = parser;
        return new Identifier { components };
    }

    public Component this[int i] => Components[i];

    public void Add(Name name) => Components.Add(name);
    public void Add(IEnumerable<Component> components) => Components.AddRange(components);

    public int Count => Components.Count;

    public void ResolveTypes(IContext context)
    {
        foreach (var component in this)
        {
            component.AsParameters?.ResolveTypes(context);
        }
    }

    public void ResolveFunctions(IContext context)
    {
        foreach (var component in this)
        {
            component.AsParameters?.ResolveFunctions(context);
        }
    }

    public void ResolveData(IContext context)
    {
        foreach (var component in this)
        {
            component.AsParameters?.ResolveData(context);
        }
    }

    public IEnumerator<Component> GetEnumerator() => Components.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Components.GetEnumerator();

    public Resolution Resolve(Reference reference)
    {
        List<Resolution> resolutions = new();

        var permutations = GeneratePermutations(this, reference);

        foreach (var permutation in permutations)
        {
            if (IsValid(permutation) is false) continue;

            System.Index previndex = default;
            System.Index prevanchor = default;
            foreach (var index in permutation)
            {
                var subreference = reference.Span[previndex..index];
                previndex = index;

                var anchor = IndexOfNextAnchorPoint(Span, prevanchor);
                var subidentifier = Span[prevanchor..anchor];
                prevanchor = anchor;

                var resolution = Subresolve(subidentifier, subreference);
                resolutions.Add(resolution);
            }
        }

        return Resolution.From(resolutions);

        static ArrayIndexPermutations GeneratePermutations(Identifier identifier, Reference reference)
        {
            ArrayIndexPermutations permutations = new();
            foreach (var idpart in identifier)
            {
                var indices = idpart.AsName is Name name
                    ? reference.IndicesOf(name)
                    : reference.IndicesOf(idpart.AsParameters);

                if (indices is null) continue;

                permutations.Add(indices);
            }
            return permutations;
        }

        static bool IsValid(System.Index[] permutation)
        {
            System.Index lastIndex = -1;
            foreach (var index in permutation)
            {
                if (lastIndex.Value < index.Value) return false;
            }
            return true;
        }

        static int IndexOfNextAnchorPoint(ReadOnlySpan<Component> identifier, System.Index start)
        {
            for (int i = start.Value + 1; i != identifier.Length; ++i)
            {
                if (identifier[i].AsParameters is Parameters and { Mandatory.Length: > 1 }) return i;
            }
            return -1;
        }
    }

    // all identifier components are Parameters where either zero or only one parameter is mandatory
    private static Resolution Subresolve(ReadOnlySpan<Component> identifier, ReadOnlySpan<Reference.Component> reference)
    {
        ArrayIndexPermutations permutations = new();
        foreach (var component in identifier)
        {
            var parameters = component.AsParameters;
            var offset = parameters.Mandatory.Length;
            var array = new System.Index[parameters.Count - offset];

            for (int i = 0; i != array.Length; ++i)
            {
                array[i] = offset + i;
            }

            permutations.Add(array);
        }

        List<Resolution> resolutions = new();
        foreach (var permutation in permutations) 
        {
            if (IsValid(permutation, reference.Length) is false) continue;

        }
        return Resolution.From(resolutions);

        static bool IsValid(System.Index[] permutation, int requiredTotal)
        {
            var total = 0;
            foreach (var index in permutation)
            {
                total += index.Value;
            }
            return total == requiredTotal;
        }
    }

    public class Component : Compiler.IParsable<Component>
    {
        private Component(Name name) => value = name;
        private Component(Parameters parameters) => value = parameters;

        public static implicit operator Component(Name name) => new(name);
        public static implicit operator Component(Parameters parameters) => new(parameters);

        public static Component Parse(ref Parser current)
        {
            if (Name.Parse(ref current) is Name name) return name;
            if (Parameters.Parse(ref current) is Parameters parameters) return parameters;
            return null;
        }

        public Name AsName => value as Name;
        public Parameters AsParameters => value as Parameters;

        private readonly object value;
    }
}