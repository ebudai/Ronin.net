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

            // NOT a read. This occurrence declares the name; reading it here is
            // what the resolver went to the trouble of ruling out, and whoever
            // introduces the scope is the one that gives it a value.
            Tree.Binding binding => Undeclared(binding),

            // brackets cost a lookup to the resolver and mean nothing here
            Tree.Group group => Grouped(graph, group, insideLet),

            Tree.Operation operation => Apply(graph, operation, insideLet),

            // Tree has exactly six shapes and Call is the sixth. Naming it and
            // adding a default would leave the default unreachable.
            _ => Invoke(graph, (Tree.Call)tree, insideLet),
        };
    }

    /// <summary>
    ///     A binding occurrence evaluated on its own, which nothing has bound.
    /// </summary>
    ///
    /// <remarks>
    ///     The construct that declares the name is the one that gives it a value,
    ///     and until the loop is a resolver production there is no such construct
    ///     here. An Error rather than a read, because a read is the specific
    ///     thing this shape exists to prevent.
    /// </remarks>
    private static object Undeclared(Tree.Binding binding)
        => new Error($"«{binding.Words}» is being declared here, and nothing has given it a value yet.");

    /// <summary>
    ///     A name a call is declaring, handed to the declaration unevaluated.
    /// </summary>
    ///
    /// <remarks>
    ///     Not an <see cref="Error"/> and not a value: a declaration receiving
    ///     one is being told which name it is introducing, and only it knows what
    ///     scope or value that name should get. Outside a call there is no such
    ///     declaration, which is why <see cref="Undeclared"/> still answers
    ///     there.
    /// </remarks>
    internal sealed record Binding(string Name);

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
    /// <remarks>
    ///     A COLLECTION is always its list, at one element as at many. Grouping
    ///     collapses, because «(x)» is x with brackets round it and the brackets
    ///     were the resolver's business. Reading them the same way made a
    ///     singleton list evaluate to its element, so «[10] @ 1» said its left
    ///     operand was not a list — and an empty one had no list to be at all.
    /// </remarks>
    private object Grouped(Graph graph, Tree.Group group, bool insideLet)
        => group.Collection || group.Parts.Count is not 1
         ? List.Of(group.Parts.Select(part => Evaluate(graph, part, insideLet)).ToArray())
         : Evaluate(graph, group.Parts[0], insideLet);

    /// <remarks>
    ///     The operator comes off the node, which is the one the resolver chose.
    ///     Looking the symbol up again in <see cref="Builtin.Operators"/> made
    ///     resolution and evaluation two registries with one name: an operator
    ///     added to a scope resolved and then had "no implementation", and an
    ///     implementation replaced in a scope was silently ignored in favour of
    ///     the built-in. There is nothing left to look up.
    /// </remarks>
    private object Apply(Graph graph, Tree.Operation operation, bool insideLet)
    {
        if (operation.Operator.Catches is not { } catches)
            return operation.Operator.Apply(Evaluate(graph, operation.Left, insideLet),
                                            Evaluate(graph, operation.Right, insideLet));

        // HANDLING, and not a plain walk. The graph remembers the first error a
        // body reads and applies it to whatever that body returns, so an
        // «otherwise» that correctly chose its fallback had the choice
        // overwritten by the very error it was asked to replace — and only when
        // the error came from another cell, which is the ordinary case and the
        // one the maintained test happened not to cover.
        var left = graph.Handling(() => Evaluate(graph, operation.Left, insideLet));

        // Not evaluated at all, rather than evaluated and discarded. Reading is
        // what records a dependency, so the branch not taken must not be read —
        // otherwise every «otherwise» makes its fallback an input of the cell it
        // is guarding, and writing to a fallback nobody wanted recomputes it.
        if (catches(left) is false) return left;

        return operation.Operator.Apply(left, Evaluate(graph, operation.Right, insideLet));
    }

    /// <summary>
    ///     A call, with its value arguments evaluated and its binding arguments
    ///     not.
    /// </summary>
    ///
    /// <remarks>
    ///     Every argument used to be evaluated, which erased the one distinction
    ///     the resolver had gone to the trouble of making: a binding occurrence
    ///     arrived as the Error saying nothing had given it a value, and the
    ///     declaration refused to run a body on an error input. So the construct
    ///     that DECLARES the name never got the chance to say what it introduces.
    ///     <para>
    ///     It reaches the declaration as a <see cref="Binding"/> instead — a
    ///     name, not a value — and what to do with it is the declaration's
    ///     business, which is where that decision belongs.
    ///     </para>
    /// </remarks>
    private object Invoke(Graph graph, Tree.Call call, bool insideLet)
        => scope.Invoke(graph,
                        call.Pattern,
                        [.. call.Arguments.Select(argument => Argument(graph, argument, insideLet))],
                        insideLet);

    private object Argument(Graph graph, Tree argument, bool insideLet)
        => argument is Tree.Binding binding
         ? new Binding(binding.Words)
         : Evaluate(graph, argument, insideLet);

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
