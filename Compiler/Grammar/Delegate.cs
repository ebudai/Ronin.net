// Copyright © 2023 Eric Budai

using OneOf;
using Ronin.Compiler;
using Ronin.Lexicon;
using System;
using System.Collections.Generic;

namespace Ronin.Grammar;

/// <summary>
///     Instance of a <see cref="Function"/> which can be assigned to a <see cref="Datum"/>
/// </summary>
/// 
/// <example>
///     var lambda = x => { return x + 3; };
///                  ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
///     var lambda = (a, b, c) => { return a + b * 3; };
///                  ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
///     var lambda = () => { return x; };
///                  ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
/// </example>
internal class Delegate : Value.Temporary
{
    public Parameters Data { get; init; }
    public Scope Definition { get; init; }

    public class Parameter : OneOfBase<Datum, Name>, IAggregable<Parameter>
    {
        protected Parameter(OneOf<Datum, Name> _) : base(_) { }

        public static implicit operator Parameter(Datum datum) => new(datum);
        public static implicit operator Parameter(Name name) => new(name);

        public static Parameter Parse(ref Parser current)
            => Datum.Parse(ref current) is Datum datum
                ? datum
                : Name.Parse(ref current);
    }

    public class Parameters : Aggregate<Parameters, OpenParenthesis, Parameter, Separator, CloseParenthesis> { }

    public static new Delegate Parse(ref Parser current)
    {
        Parser parser = current;

        if (Parameters.Parse(ref parser) is not Parameters parameters)
        {
            if (Name.Parse(ref parser) is not Name name) return null;
            parameters = new Parameters { name };
        }

        if (parser.TryAdvance<Returns>() is false) return null;

        Statement definition = null;
        if (parser.TryAdvance<Assign>())
        {
            definition = Value.Parse(ref parser);
        }
        definition ??= Scope.Parse(ref parser);

        current = parser;
        return new Delegate
        {
            Data = parameters,
            Definition = definition as Scope ?? new Scope { definition }
        };
    }
}
