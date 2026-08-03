// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System.Collections.Generic;
using System.Linq;

namespace Ronin.Grammar;

/// <summary>
///     A bracketed collection: a list, or a lookup, parsed once.
/// </summary>
///
/// <example>
///     var values  = [ 1, 2, 3 ];
///     var lookup  = [ a = 3, b = 22.3, "special" = values maximum ];
///     var nothing = [];
/// </example>
///
/// <remarks>
///     <para>
///     ONE production and one decision, which is what the design always said and
///     what the compiler did not do. A list and a lookup were separate
///     alternatives tried in order, and the cost of that is not "two attempts":
///     an association's KEY is a value, so <em>is this an association?</em>
///     cannot be answered without parsing the key — and a nested bracket inside
///     the key re-entered both alternatives again. Element attempts went as
///     2^(d+1) − 2 against depth, which is the ~600 ms at depth ten that
///     <c>MaxGroups</c> was left holding.
///     </para>
///     <para>
///     What makes one parse possible is a constraint rather than a convenience:
///     <b>«=» inside brackets is only ever an association separator, never an
///     expression operator.</b> If that ever stops being true, this stops being a
///     decision and becomes a guess, and the exponential returns through a door
///     nobody is watching.
///     </para>
///     <para>
///     The kind is decided from EVERY element and not from the first, so a mixed
///     collection is one message naming both positions rather than a parse
///     failure whose reason depends on which order the two shapes were typed in.
///     </para>
/// </remarks>
internal class Collection : Aggregate<Collection, Open.SquareBracket, Collection.Element, Separator, Close.SquareBracket>,
                            IAnswersBroken
{
    /// <summary>Whether anything under this collection failed to parse.</summary>
    ///
    /// <remarks>
    ///     Kept, because this is the only wrapper that asks — and asking per
    ///     node is what turned a walk priced for once per file into one per
    ///     nesting level. Held by the node so an enclosing collection reads it
    ///     instead of re-descending; see <see cref="IAnswersBroken"/>.
    /// </remarks>
    public bool Broken { get; private set; }

    public static new Temporary Parse(ref Parser current)
    {
        Parser start = current;

        if (Aggregate<Collection, Open.SquareBracket, Element, Separator, Close.SquareBracket>
            .Parse(ref current) is not Collection collection) return null;

        // Anything at all beneath it, and not merely the element itself. An
        // element that failed is not a list entry whatever its Origin says, so
        // «[a =, b = 2]» was classified as one value beside one association and
        // reported as mixed — hiding the syntax error and recommending a repair
        // for a mistake nobody made. Asking only about the element left the same
        // hole one level down, where the error sits under a Destination.
        //
        // Through the walk the diagnostic pass already uses, because a shallower
        // test per wrapper is what put the hole there twice.
        collection.Broken = Compilation.BrokenWithin(collection);

        if (collection.Broken) return collection;

        var associated = collection.Count(element => element.Origin is not null);

        if (associated is 0 || associated == collection.Count) return collection;

        return Mixed(collection, start.AdvanceTo(current));
    }

    /// <summary>
    ///     One entry: a value, and the value it is associated with if it has one.
    /// </summary>
    internal class Element : Statement, IParsable<Element>
    {
        public Value Destination { get; init; }
        public Value Origin { get; init; }

        public static new Element Parse(ref Parser current)
        {
            Parser parser = current;

            if (Value.Parse(ref parser) is not Value destination) return null;

            // Parsed once and asked afterwards, which is the whole change. The
            // key is a value either way, so nothing is speculative: what follows
            // it decides what this entry is.
            if (parser.TryAdvance<Assignment>() is false)
            {
                current = parser;
                return new Element { Destination = destination };
            }

            if (Value.Parse(ref parser) is not Value origin)
            {
                return new ExpectedValueError { Tokens = Parser.Recover(ref current, parser) };
            }

            current = parser;
            return new Element { Destination = destination, Origin = origin };
        }

        public class ExpectedValueError : Element, IError
        {
            public string Reason { get; } = "expected value";
            public System.ReadOnlyMemory<Token> Tokens { get; init; }
        }
    }

    private static Mismatched Mixed(Collection collection, System.ReadOnlyMemory<Token> tokens)
    {
        var values = collection.Select((element, at) => (element, at))
                               .First(entry => entry.element.Origin is null).at;

        var associations = collection.Select((element, at) => (element, at))
                                     .First(entry => entry.element.Origin is not null).at;

        return new Mismatched
        {
            Tokens = tokens,
            Value = values + 1,
            Association = associations + 1,
        };
    }

    /// <summary>A collection that is part list and part lookup.</summary>
    public class Mismatched : Collection, IError
    {
        public int Value { get; init; }
        public int Association { get; init; }

        public string Reason
            => $"entry {Value.ToString(System.Globalization.CultureInfo.InvariantCulture)} is a value and entry "
             + $"{Association.ToString(System.Globalization.CultureInfo.InvariantCulture)} is an association, so "
             + "this is neither a list nor a lookup. Give every entry a key, or none of them";

        public System.ReadOnlyMemory<Token> Tokens { get; init; }
    }
}
