// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Ronin.Grammar;

/// <summary>
///     A unique name for a <see cref="Type"/>, <see cref="Datum"/> or a <see cref="Function"/>
///     which can contain multiple <see cref="Name"/>s and <see cref="Parameters"/>
/// </summary>
internal class Identifier : IEnumerable<Identifier.Component>
{
    private List<Component> Components { get; init; } = [];

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

    /// <summary>Where the declaration was written.</summary>
    public Span Span(SourceText source)
    {
        var names = Components.Where(component => component.AsName is not null).ToArray();
        var first = names[0].AsName.Span(source);
        var last = names[^1].AsName.Span(source);

        return source.Span(first.Offset, last.Offset - first.Offset + last.Length);
    }

    /// <summary>The literal words of the declaration, space separated.</summary>
    public string Words => string.Join(' ', Components.Where(component => component.AsName is not null)
                                                     .Select(component => component.AsName.Words));

    /// <summary>
    ///     The pattern this identifier declares, and the parameter names filling
    ///     each hole. False when it declares a plain name instead.
    /// </summary>
    ///
    /// <remarks>
    ///     An identifier alternates name words and parameter blocks, and which of
    ///     the two a declaration is happens to be structural: a component that is
    ///     <see cref="Parameters"/> is a hole, and the parameters inside it are
    ///     that hole's block. So one walk produces both what the resolver needs to
    ///     read a call and what the runtime needs to bind one.
    /// </remarks>
    public bool TryPattern(out Compiler.Pattern pattern, out IReadOnlyList<IReadOnlyList<string>> blocks)
    {
        List<string> segments = [];
        List<IReadOnlyList<string>> holes = [];

        foreach (var component in Components)
        {
            if (component.AsName is Name name)
            {
                segments.AddRange(name.Words.Split(' '));
                continue;
            }

            segments.Add(null);
            holes.Add([.. component.AsParameters.Select(Named)]);
        }

        blocks = holes;

        // Width is checked HERE and not left to the constructor's guard. That
        // guard is an invariant for direct construction and throws, and a
        // declaration wide enough to trip it is ordinary source — so the bound
        // introduced to refuse hostile input would have become a fatal path of
        // its own.
        Width = segments.Count;
        IsPattern = holes.Count is not 0;

        // Having holes is what makes it a pattern, and it is the ONLY thing that
        // does. Deciding by width alone reported a 129-word plain NAME as a
        // pattern too wide, quoting a limit on a matcher it will never enter.
        if (holes.Count is 0)
        {
            pattern = null;
            return false;
        }

        pattern = segments.Count > Compiler.Pattern.MaxSegments ? null : new Compiler.Pattern(segments);

        return pattern is not null;
    }

    /// <summary>Whether the last <see cref="TryPattern"/> saw a parameter block.</summary>
    public bool IsPattern { get; private set; }

    /// <summary>
    ///     How many words and holes the last <see cref="TryPattern"/> counted,
    ///     so a caller can say why a pattern was refused rather than only that it
    ///     was.
    /// </summary>
    public int Width { get; private set; }

    /// <summary>A parameter's name, which every parameter has.</summary>
    private static string Named(Parameters.Parameter parameter) => parameter.AsDatum.Identifier.Words;

    public IEnumerator<Component> GetEnumerator() => Components.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Components.GetEnumerator();

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
