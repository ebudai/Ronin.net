// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
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

        var first = current.Token;

        // Not ParseRepeating, because the components are not interchangeable: the
        // production-keyword rule applies to the FIRST one and to nothing after
        // it. Applying it to each in turn refused «function send (x) part of
        // (y)», where no outer production has anything left to steal.
        List<Component> components = [];

        while (parser.IsNotFinished)
        {
            var component = Component.Parse(ref parser, leading: components.Count is 0);

            if (component is null) break;

            components.Add(component);
        }

        if (components.Count is 0) return null;

        var extent = first;
        for (var token = first; ReferenceEquals(token, parser.Token) is false; token = token.Next as Token)
        {
            extent = token;
        }

        current = parser;
        Identifier identifier = new();
        identifier.Add(components);

        // the parsed extent covers parameter blocks too, which the name walk
        // above cannot see
        identifier.From = first;
        identifier.To = extent;

        return identifier;
    }

    /// <summary>The first and last token it was written with.</summary>
    ///
    /// <remarks>
    ///     Kept because <see cref="Span"/> cannot always be recovered from the
    ///     name words: «function (x) rounded» begins with a parameter block, and
    ///     «function (x)» has no name words at all — both are things a person can
    ///     write and a diagnostic has to point at.
    /// </remarks>
    private Token From { get; set; }

    private Token To { get; set; }

    public Component this[int i] => Components[i];

    public void Add(Name name)
    {
        Components.Add(name);
        Extend(name);
    }

    public void Add(IEnumerable<Component> components)
    {
        Components.AddRange(components);

        foreach (var component in components)
        {
            if (component.AsName is Name name) Extend(name);
        }
    }

    /// <summary>
    ///     Widens the extent to cover a name. An identifier built by hand — a
    ///     loop's variable, a test's — never went through <see cref="Parse"/>,
    ///     and still has to be able to say where it is.
    /// </summary>
    private void Extend(Name name)
    {
        From ??= name.Tokens.Span[0];
        To = name.Tokens.Span[^1];
    }

    public int Count => Components.Count;

    /// <summary>Where the declaration was written.</summary>
    public Span Span(SourceText source)
        => source.Span(From.Offset, To.Offset + To.Memory.Length - From.Offset);

    /// <summary>
    ///     How it reads as a pattern, holes and all, whether or not it is one
    ///     this language will accept.
    /// </summary>
    public string Shape
        => string.Join(' ', Components.Select(component => component.AsName?.Words
                                                       ?? (component.AsParameters is { Count: 0 } ? "()" : "(_)")));

    /// <summary>The literal words of the declaration, space separated.</summary>
    public string Words => string.Join(' ', Canonical);

    /// <summary>
    ///     The declaration's words as the lexer counts them, which is what its
    ///     identity is. <see cref="Words"/> is a rendering of this and not the
    ///     other way round: a word may CONTAIN a space — «part of» is one token,
    ///     as «for each» is — so taking the rendering apart again recovers two
    ///     words that were never there.
    /// </summary>
    public IReadOnlyList<string> Canonical
        => [.. Components.Where(component => component.AsName is not null)
                         .SelectMany(component => component.AsName.Canonical)];

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
        var segments = Shaped;

        blocks = [.. Components.Where(component => component.AsParameters is not null)
                               .Select(component => (IReadOnlyList<string>)[.. component.AsParameters.Select(Named)])];

        // Width is checked HERE and not left to the constructor's guard. That
        // guard is an invariant for direct construction and throws, and a
        // declaration wide enough to trip it is ordinary source — so the bound
        // introduced to refuse hostile input would have become a fatal path of
        // its own.
        Width = segments.Count;
        IsPattern = blocks.Count is not 0;

        // At least one segment always: Parse refuses an identifier with no
        // components, and every component contributes one — a name its words, a
        // parameter block its hole.
        BeginsWithHole = segments[0] is null;

        // Having holes is what makes it a pattern, and it is the ONLY thing that
        // does. Deciding by width alone reported a 129-word plain NAME as a
        // pattern too wide, quoting a limit on a matcher it will never enter.
        if (IsPattern is false)
        {
            pattern = null;
            return false;
        }

        // Width BEFORE writability, because «||» is ordered and writability is
        // the expensive one: it renders the whole shape and lexes it back. A
        // bound that exists to refuse hostile input should not do that work
        // first — and the claim in Declarations that the readback "is not even
        // reached" was false while it happened here.
        pattern = BeginsWithHole || segments.Count > Compiler.Pattern.MaxSegments || Writable is false
                ? null
                : new Compiler.Pattern(segments);

        return pattern is not null;
    }

    /// <summary>Whether the last <see cref="TryPattern"/> saw a parameter block.</summary>
    public bool IsPattern { get; private set; }

    /// <summary>
    ///     Whether it began with one. Reported rather than constructed, because
    ///     the constructor's guard is an invariant for direct construction and
    ///     throws — and «function (x) rounded» is ordinary source.
    /// </summary>
    public bool BeginsWithHole { get; private set; }

    /// <summary>
    ///     How many words and holes the last <see cref="TryPattern"/> counted,
    ///     so a caller can say why a pattern was refused rather than only that it
    ///     was.
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    ///     Whether the declared words read back as the words declared.
    /// </summary>
    ///
    /// <remarks>
    ///     Computed, not recorded by <see cref="TryPattern"/>. Recorded there it
    ///     was only ever true for declarations that ask for a pattern, and a
    ///     loop variable does not — so a bracketed «for each (ready part /* gap
    ///     */ of world) in banks» kept the default and declared a name whose
    ///     rendering states different words than the declaration holds.
    /// </remarks>
    public bool Writable => Compiler.Pattern.Writable(Shaped);

    /// <summary>
    ///     Whether any bracket in this declaration is empty.
    /// </summary>
    ///
    /// <remarks>
    ///     A hole with no name. It used to become an ordinary hole, so «function
    ///     ping ()» installed «ping (_)» — which «ping» does not resolve
    ///     against, «ping ()» does not either, and «ping 1» resolves and is then
    ///     refused at binding for filling a block that holds no parameters. A
    ///     declaration with no spelling that calls it.
    /// </remarks>
    public bool HasEmptyHole => Components.Any(component => component.AsParameters is { Count: 0 });

    /// <summary>
    ///     The segments this identifier declares, a hole for each parameter
    ///     block.
    /// </summary>
    ///
    /// <remarks>
    ///     The ONE decomposition, and now actually so: <c>TryPattern</c> used to
    ///     walk the components itself while this walked them again with a
    ///     different expression, and width, writability, construction and the
    ///     diagnostic readback could each have acquired a separate opinion. An
    ///     identifier does not change after it is parsed, so it is computed once.
    /// </remarks>
    public IReadOnlyList<string> Shaped
        => shaped ??= [.. Components.SelectMany(component => component.AsName?.Canonical ?? [null])];

    private IReadOnlyList<string> shaped;

    /// <summary>
    ///     The words this identifier declares, one quotation each.
    /// </summary>
    ///
    /// <remarks>
    ///     Quoted per word and not joined, because the whole point of the
    ///     finding that uses it is that two different word sequences render
    ///     identically. Printing the renderings would show the reader the same
    ///     string twice.
    ///     <para>
    ///     A METHOD, as <see cref="Reads"/> is, because the error walk reads
    ///     every PROPERTY of every node reflectively — and «Reads» parses, which
    ///     throws for a pattern that is also too wide. A node's properties have
    ///     to be answerable; these are asked once, by the finding that wants
    ///     them.
    ///     </para>
    /// </remarks>
    public string Declares() => Boundaries(Shaped);

    /// <summary>
    ///     The words this identifier's own rendering denotes when read back.
    /// </summary>
    ///
    /// <remarks>
    ///     Through the lexical decomposition and NOT through
    ///     <c>Pattern.Parse</c>, which constructs — and the constructor enforces
    ///     the width bound by throwing. A declaration that was both unwritable
    ///     and over-width reached this while the finding for the first was being
    ///     built, so reporting the problem crashed on the other one. A finding
    ///     formatter must not cross the invariant it is reporting.
    /// </remarks>
    public string Reads() => Boundaries(Compiler.Pattern.Reads(Shaped));

    private static string Boundaries(IEnumerable<string> words)
        => "«" + string.Join("» «", words.Select(word => word ?? "(_)")) + "»";

    /// <summary>A parameter's name, which every parameter has.</summary>
    private static string Named(Parameters.Parameter parameter) => parameter.AsDatum.Identifier.Words;

    public IEnumerator<Component> GetEnumerator() => Components.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Components.GetEnumerator();

    /// <remarks>
    ///     Not <c>IParsable</c> any more. The interface exists so that
    ///     <c>ParseRepeating</c> can loop over interchangeable elements, and
    ///     components are not interchangeable: the production-keyword rule
    ///     applies to the first one and to nothing after it, which the one-argument
    ///     signature has no way to say.
    /// </remarks>
    public class Component
    {
        private Component(Name name) => value = name;
        private Component(Parameters parameters) => value = parameters;

        public static implicit operator Component(Name name) => new(name);
        public static implicit operator Component(Parameters parameters) => new(parameters);

        public static Component Parse(ref Parser current, bool leading)
        {
            if ((leading ? Name.Parse(ref current) : Name.Continuing(ref current)) is Name name) return name;
            if (Parameters.Parse(ref current) is Parameters parameters) return parameters;
            return null;
        }

        public Name AsName => value as Name;
        public Parameters AsParameters => value as Parameters;

        private readonly object value;
    }
}
