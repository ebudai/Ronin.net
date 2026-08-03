// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using System;
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
    ///     There were two — binding powers on <see cref="SymbolTable"/> and
    ///     implementations here — with a test asserting their key sets matched. A
    ///     key-set test notices a symbol added to one and not the other, and
    ///     cannot notice a precedence changed on one side or a meaning changed on
    ///     the other, which are the drifts that would actually mislead. Neither
    ///     can now happen: <see cref="SymbolTable"/> seeds from this, so adding an
    ///     operator means giving it both halves in one place.
    /// </remarks>
    public static IReadOnlyDictionary<string, Operator> Operators { get; }
        = new Dictionary<string, Operator>
        {
            ["+"] = new(10, Arithmetic("+", (left, right) => left + right)),
            ["-"] = new(10, Arithmetic("-", (left, right) => left - right)),
            ["*"] = new(20, Arithmetic("*", (left, right) => left * right)),
            ["/"] = new(20, Divide()),

            // Loosest of them, so «a + b otherwise 0» is the fallback of the
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
        };

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

            _ => new Error("«@» indexes a list"),
        });

    private static Func<object, object, object> Arithmetic(string symbol, Func<double, double, double> operation)
        => Lift((left, right) => left is double first && right is double second
                               ? operation(first, second)
                               : new Error($"«{symbol}» needs two numbers"));

    /// <summary>
    ///     The only thing that inspects a value's failure without inheriting it,
    ///     and it catches <see cref="Nothing"/> as well as <see cref="Error"/>.
    ///     This is the whole ergonomic replacement for testing every use site.
    /// </summary>
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
    public static bool Same(object left, object right) => Same(left, right, 0);

    private static bool Same(object left, object right, int depth)
    {
        // A cheap cap, kept even though the normaliser refuses a cycle at the
        // boundary. "This can never see one" is exactly the class of invariant
        // that keeps turning out to be unenforced, and one integer converts an
        // unrecoverable process death into an answer.
        if (depth > Deep) return false;

        if (left is not List first || right is not List second) return Equals(left, right);

        if (ReferenceEquals(first, second)) return true;
        if (first.Count != second.Count) return false;

        for (var at = 0; at < first.Count; ++at)
        {
            if (Same(first[at], second[at], depth + 1) is false) return false;
        }

        return true;
    }

    /// <summary>How deep a value may nest before comparison gives up.</summary>
    private const int Deep = 256;

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
