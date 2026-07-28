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

        Pattern = pattern;
        Blocks = blocks;
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

        var shape = declaration.Pattern.ToString();
        if (declarations.TryGetValue(shape, out var overloads) is false) declarations[shape] = overloads = [];

        overloads.Add(declaration);
    }

    public object Invoke(Graph graph, Pattern pattern, IReadOnlyList<object> arguments, bool insideLet)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(arguments);

        if (declarations.TryGetValue(pattern.ToString(), out var overloads) is false)
            return new Error($"no declaration for «{pattern}»");

        if (overloads.Count > 1) return new Error($"«{pattern}» is ambiguous after type filtering");

        var declaration = overloads[0];

        // where the purity rule is enforced for calls
        if (insideLet && declaration.Pure is false)
            return new Error($"«{pattern}» has effects and cannot appear in a let body");

        Dictionary<string, object> bound = [];

        foreach (var (block, argument) in declaration.Blocks.Zip(arguments))
        {
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

        return declaration.Body(graph, bound);
    }

    private readonly Dictionary<string, List<Declaration>> declarations = [];
}
