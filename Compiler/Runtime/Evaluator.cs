// Copyright © 2026 Eric Budai

using System;
using System.Globalization;
using System.Linq;
using Tree = Ronin.Compiler.Node;

namespace Ronin.Runtime;

/// <summary>
///     Walks a resolved tree and produces a value.
/// </summary>
///
/// <remarks>
///     <para>
///     This is the join. <c>Resolver</c> decides what a statement means and hands
///     back a <see cref="Tree"/>; <see cref="Graph"/> decides when anything runs
///     again; <see cref="Scope"/> knows what a pattern invokes. Nothing here
///     decides anything — it dispatches on the shape the resolver committed to.
///     </para>
///     <para>
///     <paramref name="insideLet"/> travels down the whole walk rather than being
///     recovered at the call site, because purity is a property of where a call
///     appears, not of what it is. A <c>let</c> body is pure all the way down,
///     including through the arguments of a call inside it.
///     </para>
/// </remarks>
internal sealed class Evaluator(Scope scope)
{
    public object Evaluate(Graph graph, Tree tree, bool insideLet)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(tree);

        return tree switch
        {
            Tree.Literal literal => Value(literal),

            // the join to the graph: a resolved name reference is a graph read,
            // which is also what records the dependency edge
            Tree.Name name => graph.Read(name.Words),

            // brackets cost a lookup to the resolver and mean nothing here
            Tree.Group group => Evaluate(graph, group.Inner, insideLet),

            Tree.Operation operation => Apply(graph, operation, insideLet),

            // Tree has exactly five shapes and Call is the fifth. Naming it and
            // adding a default would leave the default unreachable.
            _ => Invoke(graph, (Tree.Call)tree, insideLet),
        };
    }

    /// <summary>
    ///     Wraps a tree as a <c>let</c> body. Everything inside is evaluated as
    ///     pure, and the graph re-runs it whenever something it read changed.
    /// </summary>
    public Func<Graph, object> Body(Tree tree)
    {
        ArgumentNullException.ThrowIfNull(tree);

        return graph => Evaluate(graph, tree, insideLet: true);
    }

    private object Apply(Graph graph, Tree.Operation operation, bool insideLet)
    {
        // The resolver only builds an Operation for a symbol in its own operator
        // table, so this only fires for a hand-built tree or if the two tables
        // drift apart.
        if (Builtin.Operators.TryGetValue(operation.Symbol, out var apply) is false)
            return new Error($"«{operation.Symbol}» has no implementation");

        return apply(Evaluate(graph, operation.Left, insideLet),
                     Evaluate(graph, operation.Right, insideLet));
    }

    private object Invoke(Graph graph, Tree.Call call, bool insideLet)
        => scope.Invoke(graph,
                        call.Pattern,
                        [.. call.Arguments.Select(argument => Evaluate(graph, argument, insideLet))],
                        insideLet);

    /// <summary>
    ///     A literal denotes itself, which is why it costs the resolver nothing.
    ///     Only numbers and text are read so far; a date lexes and resolves but
    ///     has no runtime value yet, and says so rather than pretending.
    /// </summary>
    private static object Value(Tree.Literal literal)
    {
        var text = literal.Text;

        if (text[0] is '"') return text[1..^1];

        return double.TryParse(text,
                               NumberStyles.Float | NumberStyles.AllowThousands,
                               CultureInfo.InvariantCulture,
                               out var number)
             ? number
             : new Error($"«{text}» is a literal the interpreter does not read yet");
    }
}
