// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System;
using System.Collections.Generic;

namespace Ronin.Grammar;

/// <summary>
///     Aggregate of <see cref="Datum"/> used to declare variables to enter into a <see cref="Function"/>
/// </summary>
/// 
/// <remarks>
///     <see cref="Separator"/>-separated <see cref="Datum"/>s between <see cref="Open.Parenthesis"/> and <see cref="Close.Parenthesis"/>
/// </remarks>
/// 
/// <example>
///     function thing (x => number, var y => text) with stuff { return 8; }
///                    ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
/// </example>
internal class Parameters : Aggregate<Parameters, Open.Parenthesis, Parameters.Parameter, Separator, Close.Parenthesis>
{
    /// <summary>
    ///     A parameter is a declaration and nothing else.
    /// </summary>
    ///
    /// <remarks>
    ///     It used to fall through to <see cref="Association"/>, because
    ///     <c>Datum.Parse</c> rejected «order = 3» on a guard that belongs to
    ///     statement position. With the parameter path in place that fallback is
    ///     unreachable — and it was never right, since an association here has a
    ///     name the binder cannot see.
    /// </remarks>
    public class Parameter : Compiler.IParsable<Parameter>
    {
        private Parameter(Datum datum) => AsDatum = datum;

        public static implicit operator Parameter(Datum datum) => new(datum);

        // Not a ternary: «is Datum d ? d : null» types as Datum and then converts
        // the RESULT, wrapping a null datum in a non-null Parameter.
        public static Parameter Parse(ref Parser current)
        {
            if (Datum.Parameter(ref current) is not Datum datum) return null;

            return datum;
        }

        public Datum AsDatum { get; }
    }
}