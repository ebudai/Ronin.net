// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ronin.Runtime;

/// <summary>
///     Binds a <see cref="Pattern"/> to something invokable.
/// </summary>
///
/// <remarks>
///     <para>
///     The resolver produces <c>Node.Call(pattern, arguments)</c>, and a pattern
///     is only a shape — it says how a statement reads, not what it does. This is
///     the missing link, and it is established at declaration time.
///     </para>
///     <para>
///     A hole is one parameter <em>block</em>, not one parameter: the language
///     allows «(x, y)» and allows the brackets to be dropped when fewer than two
///     parameters are bound. The resolver therefore hands over exactly one
///     argument per hole and the binder destructures, which keeps the resolver
///     ignorant of arity.
///     </para>
/// </remarks>
internal sealed class Declaration
{
    public Declaration(
        Pattern pattern,
        IReadOnlyList<IReadOnlyList<string>> blocks,
        Func<Graph, IReadOnlyDictionary<string, object>, object> body,
        bool pure = true)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(blocks);
        ArgumentNullException.ThrowIfNull(body);

        var holes = pattern.Segments.Count(segment => segment is null);
        if (blocks.Count != holes)
            throw new ArgumentException($"«{pattern}» has {holes} hole(s) and {blocks.Count} block(s)", nameof(blocks));

        // Every parameter name distinct, across the whole declaration and not
        // merely within one block. Binding writes them into a dictionary, so a
        // repeat is not an error there — the second value silently replaces the
        // first and the body reads one argument where two were passed. The
        // declaration pass refuses the source that would do this; this is the
        // invariant for anything constructing a Declaration directly.
        // Every hole binds at least one name. A block with none passes a
        // duplicate check vacuously, and that is how «ping (_)» survived: a
        // pattern with a hole and a block that binds nothing, which no ordinary
        // argument can fill. The source rule refuses «function ping ()»; this
        // is the invariant for anything building a Declaration directly.
        if (blocks.Any(block => block is null || block.Count is 0))
            throw new ArgumentException($"«{pattern}» has a hole that binds no name", nameof(blocks));

        var named = blocks.SelectMany(block => block).ToArray();

        if (named.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException($"«{pattern}» has a parameter with no name", nameof(blocks));

        if (named.Distinct(StringComparer.Ordinal).Count() != named.Length)
            throw new ArgumentException($"«{pattern}» names a parameter twice, and a binding would keep one",
                                        nameof(blocks));

        Pattern = pattern;

        // deep, because both levels are the caller's and the inner ones are what
        // a binding actually reads
        Blocks = [.. blocks.Select(block => (IReadOnlyList<string>)[.. block])];

        Body = body;
        Pure = pure;
    }

    public Pattern Pattern { get; }

    /// <summary>The parameter names filling each hole, in hole order.</summary>
    public IReadOnlyList<IReadOnlyList<string>> Blocks { get; }

    public Func<Graph, IReadOnlyDictionary<string, object>, object> Body { get; }

    /// <summary>Whether it may appear inside a <c>let</c> body.</summary>
    public bool Pure { get; }
}

/// <summary>
///     The declarations in scope, keyed by pattern shape.
/// </summary>
///
/// <remarks>
///     A shape maps to a <em>list</em>, because overloads share a shape and are
///     separated later by type. The phase order is enumerate readings, filter by
///     type, rank by lookup count, and a surviving tie is an error — so the
///     resolver must be able to hand back several candidates and only the type
///     filter may cut them.
/// </remarks>
internal sealed class Scope
{
    public void Declare(Declaration declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);

        if (declarations.TryGetValue(declaration.Pattern, out var overloads) is false)
            declarations[declaration.Pattern] = overloads = [];

        overloads.Add(declaration);
    }

    public object Invoke(Graph graph, Pattern pattern, IReadOnlyList<object> arguments, bool insideLet)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(arguments);

        if (declarations.TryGetValue(pattern, out var overloads) is false)
            return new Error($"no declaration for «{pattern}»");

        if (overloads.Count > 1) return new Error($"«{pattern}» is ambiguous after type filtering");

        var declaration = overloads[0];

        // where the purity rule is enforced for calls
        if (insideLet && declaration.Pure is false)
            return new Error($"«{pattern}» has effects and cannot appear in a let body");

        // Zip would drop the tail of whichever side is longer: too many arguments
        // vanish, too few leave names unbound for the body to fail on later.
        if (arguments.Count != declaration.Blocks.Count)
            return new Error(
                $"«{pattern}» takes {declaration.Blocks.Count} argument(s) and was given " +
                $"{arguments.Count}.");

        Dictionary<string, object> bound = [];

        foreach (var (block, raw) in declaration.Blocks.Zip(arguments))
        {
            // Normalised on the way IN as well as out. An argument is a value
            // arriving from outside the declaration, and a host that calls this
            // directly hands over an array it still holds — the same ingress
            // the type seeds were missing, one call frame along.
            var argument = List.Admit(raw);

            // Before the shape is classified. A refused argument is not an
            // «IReadOnlyList», so a cyclic one handed to a two-parameter block
            // was reported as "given a single argument" — the real failure lost
            // and the message recommending a repair for a mistake nobody made.
            // Per argument, so an earlier failure cannot be hidden by a later
            // argument's shape.
            if (argument is Error refused) return refused;

            if (block.Count is 1)
            {
                bound[block[0]] = argument;
                continue;
            }

            if (argument is not IReadOnlyList<object> group)
                return new Error($"«{pattern}» binds {block.Count} parameters here and was given a single argument");

            if (group.Count != block.Count)
                return new Error($"«{pattern}» binds {block.Count} parameters here and was given {group.Count}");

            foreach (var (name, value) in block.Zip(group)) bound[name] = value;
        }

        // bodies never run on error inputs
        foreach (var value in bound.Values)
        {
            if (value is Error error) return error;
        }

        // Normalised HERE and not only where the graph body finishes. A
        // declaration returning «new object[] { 7 }» is the host-facing way to
        // build a list, and «(f) @ 1» asked the operator about it long before
        // the surrounding graph body returned — so the operator saw the
        // representation the runtime had stopped accepting and refused it.
        return List.Admit(declaration.Body(graph, bound));
    }

    private readonly Dictionary<Pattern, List<Declaration>> declarations = [];
}
