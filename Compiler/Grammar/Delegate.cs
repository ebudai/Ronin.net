// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
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
internal class Delegate : Temporary
{
    public Parameters Data { get; init; }
    public Scope Definition { get; init; }

    public static new Delegate Parse(ref Parser current)
    {
        Parser parser = current;

        if (Parameters.Parse(ref parser) is not Parameters parameters)
        {
            if (Name.Parse(ref parser) is Name name)
            {
                parameters = [name];
            }
            else if (parser.TryAdvance<Open.Parenthesis>() && parser.TryAdvance<Close.Parenthesis>())
            {
                parameters = [];
            }
            else
            {
                return null;
            }
        }

        if (parser.TryAdvance<Returns>() is false) return null;
        if (Scope.Definition.Parse(ref parser) is not Scope definition) return null;

        current = parser;
        return new Delegate
        {
            Data = parameters,
            Definition = definition
        };
    }

    public class Parameter : IParsable<Parameter>
    {
        protected Parameter(Datum datum) => value = datum;
        protected Parameter(Name name) => value = name;

        public static implicit operator Parameter(Datum datum) => new(datum);
        public static implicit operator Parameter(Name name) => new(name);

        public static Parameter Parse(ref Parser current)
        {
            if (Datum.Parse(ref current) is Datum datum) return datum;
            if (Name.Parse(ref current) is Name name) return name;
            return null;
        }

        public Datum AsDatum => value as Datum;
        public Name AsName => value as Name;

        private readonly object value;
    }

    public class Parameters : Aggregate<Parameters, Open.Parenthesis, Parameter, Separator, Close.Parenthesis>
    {
        public class UnresolvedDatumError : Parameter, IError
        {
            public UnresolvedDatumError(Datum.Unresolved unresolved) : base(unresolved) 
            {
                List<Token> tokens = new();
                foreach (var component in unresolved.Reference)
                {
                    // component must be a Name as it's a datum and they can't have parameters
                    tokens.AddRange(component.AsName.Tokens.ToArray());
                }
                Tokens = tokens.ToArray();
            }

            public string Reason { get; } = "unresolved datum";
            public System.ReadOnlyMemory<Token> Tokens { get; }
        }
    }
}