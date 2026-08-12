// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Runtime;

/// <summary>
///     A failure that flows through the graph as a value, the way <c>#DIV/0!</c>
///     flows through a spreadsheet.
/// </summary>
///
/// <remarks>
///     A node whose dependency is an error becomes an error <em>without running
///     its body</em>, so nothing has to be written defensively against upstream
///     failure. Fixing the source dirties everything downstream and the error
///     clears itself.
/// </remarks>
internal class Error(string message)
{
    public string Message { get; } = message;

    public override string ToString() => $"error({Message})";

    /// <summary>
    ///     Two failures of the same kind saying the same thing are the same
    ///     value.
    /// </summary>
    ///
    /// <remarks>
    ///     Cutoff compares a recompute's result with the cached one and stops
    ///     propagating when they match. Reference equality means a body that
    ///     fails the same way every round produces a NEW error every round, so
    ///     the clock advances, dependents wake, and the graph never goes quiet —
    ///     which is exactly what cutoff exists to prevent, arriving by a
    ///     different door. A removed instance's readers would do that for ever.
    ///     <para>
    ///     By kind and by message, and nothing else. If an error ever carries a
    ///     site or a time, equality must not follow it there for the same
    ///     reason.
    ///     </para>
    /// </remarks>
    public override bool Equals(object other)
        => other is Error failure && failure.GetType() == GetType() && failure.Message == Message;

    /// <remarks>
    ///     Required by the contract and used by nothing: a failure is compared,
    ///     never keyed. Written rather than omitted because a type that
    ///     overrides one and not the other is a trap for whoever first puts an
    ///     error in a set.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public override int GetHashCode() => System.HashCode.Combine(GetType(), Message);
}

/// <summary>
///     A defect in the interpreter, as distinct from a failure in the program.
/// </summary>
///
/// <remarks>
///     Caught so a live session survives one bad node, and tagged so it can never
///     be mistaken for a result. Catching everything and calling it an
///     <see cref="Error"/> would make the interpreter undebuggable — every null
///     reference in the evaluator would surface as a user-facing spreadsheet
///     error, indistinguishable from a real division by zero. And
///     <see cref="Builtin.Otherwise"/> must not catch one: a fallback for a
///     program error is a fallback, a fallback for an interpreter bug is a
///     hidden crash.
/// </remarks>
internal sealed class Fault(string message) : Error(message)
{
    public override string ToString() => $"fault({Message})";
}

/// <summary>The absence of a value, distinct from an <see cref="Error"/>.</summary>
internal sealed class Nothing
{
    public static readonly Nothing Instance = new();

    private Nothing() { }

    public override string ToString() => "nothing";
}

/// <summary>
///     Thrown when a <c>let</c> body does something only a <c>var</c> may do.
/// </summary>
///
/// <remarks>
///     An exception rather than a returned error because the violation happens
///     arbitrarily deep inside a body that has no way to report upward.
///     <see cref="Graph.Recompute"/> catches it and turns it back into a value at
///     the boundary, so nothing outside the runtime ever sees the throw.
/// </remarks>
[ExcludeFromCodeCoverage]
internal sealed class PurityViolation(string message) : Exception(message);

internal static class Builtin
{
    /// <summary>
    ///     Wraps an operation so it propagates errors rather than running on
    ///     them. Every builtin is lifted; <see cref="Otherwise"/> is the single
    ///     exception.
    /// </summary>
    public static Func<object, object, object> Lift(Func<object, object, object> operation)
        => (left, right) => left as Error ?? right as Error ?? operation(left, right);

    /// <summary>
    ///     The operators. One table: precedence, associativity and meaning
    ///     together, because they are one fact about the language.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     There were two — binding powers on <see cref="SymbolTable"/> and
    ///     implementations here — with a test asserting their key sets matched. A
    ///     key-set test notices a symbol added to one and not the other, and
    ///     cannot notice a precedence changed on one side or a meaning changed on
    ///     the other, which are the drifts that would actually mislead. Neither
    ///     can now happen: <see cref="SymbolTable"/> seeds from this, so adding an
    ///     operator means giving it both halves in one place.
    ///     </para>
    ///     <para>
    ///     FROZEN, because "one fact about the language" was a mutable
    ///     dictionary with a read-only type in front of it. One cast removed
    ///     «+» for every resolver built afterwards — a scope may extend its own
    ///     table, which is deliberate, and nothing may edit the definition
    ///     everything else copies from.
    ///     </para>
    /// </remarks>
    public static IReadOnlyDictionary<string, Operator> Operators { get; }
        = new Dictionary<string, Operator>
        {
            ["+"] = new(10, Arithmetic("+", (left, right) => left + right)),
            ["-"] = new(10, Arithmetic("-", (left, right) => left - right)),
            ["*"] = new(20, Arithmetic("*", (left, right) => left * right)),
            ["/"] = new(20, Divide()),

            // The language's equality, and the same function cutoff, «changes»
            // and «old» already ask — one comparison rather than two that can
            // disagree. For anything with identity, identity IS its equality, so
            // this needs no reference-equality partner: two boxes with equal
            // members are two boxes, and «is» on a handle says so by comparing
            // handles.
            //
            // FIVE, and the number is measured rather than analogous. Below
            // «PatternBindingPower» at 7, or «sum of a is b» reads as
            // «sum of (a is b)» — a trailing free hole parses its argument at
            // the pattern's own level, so the pattern swallows every comparison
            // written after a call. And below «otherwise» at 6, or
            // «a is total otherwise 0» reads as «(a is total) otherwise 0» —
            // the fallback catching a truth, which can never be nothing, when
            // the thing that might be nothing is «total».
            //
            // 1 TO 4 ARE RESERVED for «and» and «or», which must be looser than
            // comparison so «a is b and c is d» groups as two comparisons.
            // Written down because nothing distinguishes 5 from 1 today — they
            // do not exist yet — and the next person to look would compact it
            // and take the room with it.
            ["is"] = new(5, Lift((left, right) => Same(left, right))),

            // The fallback level, and looser than everything it guards, so
            // «a + b otherwise 0» is the fallback of the
            // sum and not the sum of a fallback, and «a otherwise b + c» falls
            // back to the whole sum: what it guards is the expression beside it,
            // which is the only reading that makes it worth writing.
            //
            // BELOW the pattern binding power, which is where a plumbing
            // operator belongs and what nine got wrong. A word pattern is
            // available only where the requested minimum is at most its own
            // level, so an operator above it takes the call's last argument
            // instead of its result: «parse input otherwise standby» read as
            // «parse («input» otherwise «standby»)», guarding the argument and
            // then calling with it, and the mirror «input otherwise parse
            // standby» would not resolve at all.
            //
            // Looser than «is» too, at five, so «a is total otherwise 0» falls
            // back on «total» rather than on the comparison — a truth can never
            // be nothing, so guarding one guards nothing.
            //
            // Six and not something rounder for the same reason nine was chosen:
            // a level is not free. The resolver derives its table from the
            // powers the operators use and each new one widens every span, so
            // six borrows the seven that patterns already need and costs one
            // column where a level with nothing adjacent costs two.
            ["otherwise"] = new(6, Otherwise) { Catches = Replaces },

            // Tighter than arithmetic, so «list @ 4 + 1» is «(list @ 4) + 1»:
            // what is indexed is the list beside it and not the sum. Twenty-one
            // borrows the column «*» already needs for the side that may not
            // repeat it, so it costs one where a level with nothing adjacent
            // costs two.
            ["@"] = new(21, Indexing()),
        }.ToFrozenDictionary();

    /// <summary>
    ///     Division, which is the one arithmetic operation with a case that has
    ///     no answer.
    /// </summary>
    ///
    /// <remarks>
    ///     An infinity would satisfy the hardware and then poison everything
    ///     downstream silently, which is precisely the spreadsheet failure the
    ///     error model exists to make visible. The error stops it here instead.
    /// </remarks>
    private static Func<object, object, object> Divide()
        => Lift((left, right) => (left, right) switch
        {
            (double, 0d) => new Error(
                "«/» cannot divide by zero. There is no value to return, and an infinity " +
                "would travel silently through every reader. Guard the divisor, or supply a " +
                "fallback with «otherwise»."),

            (double first, double second) => first / second,

            _ => new Error("«/» needs two numbers"),
        });

    /// <summary>
    ///     Indexing, which is ONE-BASED and closed.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     A symbol and not a word. A word-spelled indexer would put its glue in
    ///     the reserved set and end «RESERVED (0)», which is a property this
    ///     language has and few others do.
    ///     </para>
    ///     <para>
    ///     Every way of missing is a value and not a throw, for the reason
    ///     division by zero is: an index past the end is an ordinary thing for a
    ///     program to compute, and a fallback is already the language's answer to
    ///     it. «list @ 4 otherwise 0» reads as what it does.
    ///     </para>
    /// </remarks>
    private static Func<object, object, object> Indexing()
        => Lift((left, right) => (left, right) switch
        {
            (List list, double position) when position != System.Math.Floor(position)
                => new Error($"«@» takes a whole position, and {position} is not one"),

            // Said separately from the range, because off-by-one is the mistake
            // this spelling exists to make unlikely and «0» is what someone
            // arriving from a zero-based language writes first.
            (List, 0d) => new Error("«@» counts from one, so there is no position 0. The first is «@ 1»."),

            (List list, double position) when position < 1 || position > list.Count
                => new Error($"«@» has no position {position} in a list of {list.Count}"),

            (List list, double position) => list[(int)position - 1],

            (List, _) => new Error("«@» takes a number for a position"),

            // ONE key relation, the same «is» that equality and the duplicate-key
            // refusal use. A lookup accelerated by a host hash table would answer
            // a structural key that IS a key in it with "not found", and the
            // disagreement stays invisible until someone uses a compound key.
            (Lookup lookup, _) => Found(lookup, right),

            _ => new Error("«@» indexes a list or a lookup"),
        });

    /// <summary>
    ///     The value under a key, or the failure of there not being one.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     A MISS is NOTHING, and «m @ k» is typed «optional V» — which is what
    ///     makes a forgotten miss a compile-time error rather than a runtime one,
    ///     because «optional V» is not «V» and «m @ k + 1» does not type-check. It
    ///     also keeps a «match» exhaustive by ordinary typing: arms covering every
    ///     case give «T» and arms missing one give «optional T», where an error
    ///     is in no «T» at all. Optionals nest, so this still tells ABSENT from
    ///     present-and-nothing: absent is nothing at the outer level.
    ///     </para>
    ///     <para>
    ///     A list index out of range stays an ERROR, and the difference is in kind
    ///     rather than in taste: a missing key is data, a question about a table
    ///     with an honest answer, while an index past the end of a list is a
    ///     mistake. Typing «xs @ i» as «optional T» would put an «otherwise» on
    ///     every list index in the language to pay for a case that is a bug
    ///     wherever it happens.
    ///     </para>
    ///     <para>
    ///     A walk, and the comparison that decides is «is» — the one the
    ///     duplicate-key refusal and equality also use, so a structural key that
    ///     IS a key in the table is found rather than missed by a hash that
    ///     disagrees with the language.
    ///     </para>
    /// </remarks>
    private static object Found(Lookup lookup, object key)
    {
        foreach (var (candidate, value) in lookup)
        {
            if (Same(candidate, key)) return value;
        }

        return Nothing.Instance;
    }

    private static Func<object, object, object> Arithmetic(string symbol, Func<double, double, double> operation)
        => Lift((left, right) => left is double first && right is double second
                               ? operation(first, second)
                               : new Error($"«{symbol}» needs two numbers"));

    /// <summary>
    ///     The language's equality: value equality, all the way down.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     A list is a VALUE, so two lists with the same elements are the same
    ///     list. .NET arrays compare by reference, which made a list-valued cell
    ///     that recomputed to the same contents look changed — so cutoff never
    ///     fired on one and every downstream reader woke on every tick.
    ///     </para>
    ///     <para>
    ///     Unconditional, and with an EARLY EXIT, which is what makes that
    ///     affordable: the full n is paid only when the lists are equal, and that
    ///     is exactly the case where the saving is banked. Two that differ
    ///     usually part at the first element. Against a measured 97% of
    ///     recomputes producing an unchanged value, comparison loses only where
    ///     the list is long AND the downstream is small, which is a narrow band
    ///     and a digest's job if a measurement ever asks for one.
    ///     </para>
    ///     <para>
    ///     Interning would give O(1) forever and is refused: it needs a global
    ///     table, which in an always-running session is never collected, and it
    ///     is a synchronisation point in a design whose parallel section was
    ///     built to have none.
    ///     </para>
    ///     <para>
    ///     A LOOKUP is a different function — it is unordered, so two with the
    ///     same associations in a different order are the same lookup, and
    ///     reusing this would call them different. Not written here because a
    ///     lookup has no runtime value yet: «[a = 1]» does not resolve. It has to
    ///     arrive with one.
    ///     </para>
    /// </remarks>
    public static bool Same(object left, object right)
    {
        // No depth cap. One here returned FALSE for two equal lists that the
        // runtime had accepted, which is not an equivalence and is visible
        // through cutoff, «changes» and «old». A list is refused at
        // construction if it nests past what this can follow, so everything
        // that reaches here is comparable.
        HashSet<(object Left, object Right)> proven = null;

        return Same(left, right, ref proven);
    }

    /// <summary>
    ///     Two values, carrying the pairs already proved through every aggregate
    ///     kind.
    /// </summary>
    ///
    /// <remarks>
    ///     ONE context across both kinds, because a shared subtree reached by two
    ///     paths must be proved once however the kinds alternate on the way down.
    ///     A memo per kind is no memo at all where a lookup's value is a list whose
    ///     element is a lookup — which is exactly the shape admission preserves
    ///     when it keeps a host DAG shared rather than expanding it.
    /// </remarks>
    private static bool Same(object left, object right, ref HashSet<(object Left, object Right)> proven)
    {
        if (left is List first && right is List second) return Same(first, second, ref proven);

        // A lookup is compared as a SEQUENCE, because admission sorted it into a
        // canonical order — so this is the list comparison over a canonical form
        // rather than a second equality that could disagree with it. A list beside
        // a lookup is never the same, which the fall-through answers.
        if (left is Lookup keyed && right is Lookup beside) return Same(keyed, beside, ref proven);

        return Equals(left, right);
    }

    /// <summary>
    ///     Two lookups, compared as maps rather than as sequences.
    /// </summary>
    ///
    /// <remarks>
    ///     UNORDERED, because a lookup is a map: the same keys with the same value
    ///     at each is the whole of it, and the order they were written in is not
    ///     part of the value. Keys are distinct within a lookup, so a key on one
    ///     side matches at most one on the other and equal counts with every key
    ///     matched is a bijection.
    /// </remarks>
    private static bool Same(Lookup first, Lookup second, ref HashSet<(object Left, object Right)> proven)
    {
        if (ReferenceEquals(first, second)) return true;
        if (first.Count != second.Count) return false;
        if (proven?.Add((first, second)) is false) return true;

        foreach (var (key, value) in first)
        {
            if (Aggregate(key) || Aggregate(value)) proven ??= [(first, second)];

            var matched = false;

            foreach (var (candidate, against) in second)
            {
                if (Same(key, candidate, ref proven) is false) continue;
                if (Same(value, against, ref proven) is false) return false;

                matched = true;
                break;
            }

            if (matched is false) return false;
        }

        return true;
    }

    /// <summary>Whether descending here could meet a pair worth remembering.</summary>
    private static bool Aggregate(object value) => value is List or Lookup;

    /// <summary>
    ///     Two lists, comparing each shared pair once.
    /// </summary>
    ///
    /// <remarks>
    ///     <para>
    ///     Admission keeps a host DAG shared rather than expanding it into a
    ///     tree, which would move the same exponential here the moment two
    ///     independently admitted DAGs met: each equal subtree would be
    ///     re-proved once per path that reaches it.
    ///     </para>
    ///     <para>
    ///     Assuming a repeat pair equal is sound because a cycle is refused at
    ///     admission. With no cycles, a pair met twice is one already PROVED —
    ///     nothing can still be in progress above it — and an unequal pair
    ///     returns immediately rather than being recorded.
    ///     </para>
    ///     <para>
    ///     The set is allocated only on the first descent into a nested list, so
    ///     a flat list — the overwhelmingly common case, and the one cutoff runs
    ///     on every settle — allocates nothing.
    ///     </para>
    /// </remarks>
    private static bool Same(List first, List second, ref HashSet<(object Left, object Right)> proven)
    {
        if (ReferenceEquals(first, second)) return true;
        if (first.Count != second.Count) return false;
        if (proven?.Add((first, second)) is false) return true;

        for (var at = 0; at < first.Count; ++at)
        {
            if (Aggregate(first[at])) proven ??= [(first, second)];

            if (Same(first[at], second[at], ref proven) is false) return false;
        }

        return true;
    }

    /// <summary>
    ///     The only thing that inspects a value's failure without inheriting it,
    ///     and it catches <see cref="Nothing"/> as well as <see cref="Error"/>.
    ///     This is the whole ergonomic replacement for testing every use site.
    /// </summary>
    public static object Otherwise(object value, object fallback) => Replaces(value) ? fallback : value;

    /// <summary>
    ///     Which values a fallback replaces, asked before the fallback exists.
    /// </summary>
    ///
    /// <remarks>
    ///     ONE predicate, because it was two and they had already diverged. A
    ///     <see cref="Fault"/> IS an <see cref="Error"/>, so "does this need a
    ///     fallback" said yes to one while "does the fallback win" said no — and
    ///     the fallback was evaluated, and became a dependency, of a cell no
    ///     value of it could ever repair.
    ///     <para>
    ///     A fault is not caught. It is a defect in a body rather than a value a
    ///     program computed, and papering over one is how a defect becomes a
    ///     wrong answer instead of a report.
    ///     </para>
    /// </remarks>
    public static bool Replaces(object value) => value is Error and not Fault or Nothing;
}
