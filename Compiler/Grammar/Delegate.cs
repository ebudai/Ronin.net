// Copyright © 2023 Eric Budai

using OneOf;
using Ronin.Compiler;
using Ronin.Lexicon;
using System;
using System.Collections.Generic;

namespace Ronin.Grammar;

/// <summary>
///     Instance of a <see cref="Function.Declaration"/> which can be assigned to a <see cref="Datum"/>
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

//using Parameter = Grammar<Datum, Name>;
//using Definition = Grammar<Scope, Value>;


internal class Delegate : Value.Temporary, IGrammar<Delegate>
{
    public Parameters Data { get; init; }
    public Definition Definition { get; init; }

    public class Parameter : OneOfBase<Datum, Name>, IGrammar<Parameter>
    {
        protected Parameter(OneOf<Name, Datum> _) : base(_) { }

        public static implicit operator Parameter(Datum datum) => datum;
        public static implicit operator Parameter(Name name) => name;

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

        Definition definition = parser.TryAdvance<Assign>()
            ? Value.Parse(ref parser)
            : Scope.Parse(ref parser);

        current = parser;
        return new Delegate
        {
            Data = parameters,
            Definition = definition
        };
    }

    
}
