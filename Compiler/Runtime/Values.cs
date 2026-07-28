// Copyright © 2026 Eric Budai

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
    ///     What each operator does. The resolver's <c>SymbolTable.Operators</c>
    ///     gives the same symbols their binding power; the two tables are separate
    ///     and must agree, which is why a test pins that they do.
    /// </summary>
    public static IReadOnlyDictionary<string, Func<object, object, object>> Operators { get; }
        = new Dictionary<string, Func<object, object, object>>
        {
            ["+"] = Arithmetic("+", (left, right) => left + right),
            ["-"] = Arithmetic("-", (left, right) => left - right),
            ["*"] = Arithmetic("*", (left, right) => left * right),
            ["/"] = Divide(),
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

    private static Func<object, object, object> Arithmetic(string symbol, Func<double, double, double> operation)
        => Lift((left, right) => left is double first && right is double second
                               ? operation(first, second)
                               : new Error($"«{symbol}» needs two numbers"));

    /// <summary>
    ///     The only thing that inspects a value's failure without inheriting it,
    ///     and it catches <see cref="Nothing"/> as well as <see cref="Error"/>.
    ///     This is the whole ergonomic replacement for testing every use site.
    /// </summary>
    public static object Otherwise(object value, object fallback)
        => value is Fault ? value
         : value is Error or Nothing ? fallback
         : value;
}
