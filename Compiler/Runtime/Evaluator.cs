// Copyright © 2026 Eric Budai

using System;
using System.Globalization;
using System.Linq;
using System.Text;
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
            Tree.Group group => Grouped(graph, group, insideLet),

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

    /// <summary>
    ///     A group of one is just its contents. A group of several is the list a
    ///     parameter block of the same size destructures, so the brackets that
    ///     could not be dropped are exactly the ones that carry meaning.
    /// </summary>
    private object Grouped(Graph graph, Tree.Group group, bool insideLet)
        => group.Parts.Count is 1
         ? Evaluate(graph, group.Parts[0], insideLet)
         : group.Parts.Select(part => Evaluate(graph, part, insideLet)).ToArray();

    /// <remarks>
    ///     The operator comes off the node, which is the one the resolver chose.
    ///     Looking the symbol up again in <see cref="Builtin.Operators"/> made
    ///     resolution and evaluation two registries with one name: an operator
    ///     added to a scope resolved and then had "no implementation", and an
    ///     implementation replaced in a scope was silently ignored in favour of
    ///     the built-in. There is nothing left to look up.
    /// </remarks>
    private object Apply(Graph graph, Tree.Operation operation, bool insideLet)
        => operation.Operator.Apply(Evaluate(graph, operation.Left, insideLet),
                                    Evaluate(graph, operation.Right, insideLet));

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

        if (text[0] is '"') return Unescaped(text[1..^1]);

        return double.TryParse(text,
                               NumberStyles.Float | NumberStyles.AllowThousands,
                               CultureInfo.InvariantCulture,
                               out var number)
             ? number
             : new Error($"«{text}» is a literal the interpreter does not read yet");
    }

    /// <summary>
    ///     A text literal's value, with its escapes applied.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     The quotes were stripped and nothing else was, so the escapes the lexer
    ///     goes to some trouble to recognise survived into the value: «"a\""» was
    ///     four characters ending in a backslash and a quote rather than two
    ///     ending in a quote. Every text containing one meant something other than
    ///     what it spelled.
    ///     </para>
    ///     <para>
    ///     Two escapes, because two are what the lexer knows: a quote that does
    ///     not close the literal and the backslash that lets it not. Anything else
    ///     is an error rather than a passed-through backslash — «\n» has no
    ///     meaning yet, and silently making it mean backslash-n is what would stop
    ///     it ever meaning a newline.
    ///     </para>
    /// </remarks>
    private static object Unescaped(string text)
    {
        if (text.Contains('\\') is false) return text;

        StringBuilder value = new(text.Length);

        for (var i = 0; i < text.Length; ++i)
        {
            if (text[i] is not '\\')
            {
                value.Append(text[i]);
                continue;
            }

            // the lexer cannot produce a trailing backslash: it would have
            // escaped the closing quote, and the literal would not have closed
            var escape = text[i + 1];

            if (escape is not ('\\' or '"'))
                return new Error($"«\\{escape}» is not an escape this language has. " +
                                 "«\\\\» is a backslash and «\\\"» is a quote; write a backslash " +
                                 "as «\\\\» if that is what was meant.");

            value.Append(escape);
            ++i;
        }

        return value.ToString();
    }
}
