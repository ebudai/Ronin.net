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
    public ReadOnlyMemory<Parameter> Mandatory { get; }

    public Parameters() : base()
    {
        List<Parameter> parameters = new();
        foreach (var parameter in this)
        {
            if (parameter.AsDatum is not Datum datum) continue;
            if (datum.Initializer is not null) continue;
            if (datum.Modifiers.Is<Optional>()) continue;
            parameters.Add(parameter);
        }
        Mandatory = parameters.ToArray();
    }

    public class Parameter : Compiler.IParsable<Parameter>
    {
        private Parameter(Datum datum) => value = datum;
        private Parameter(Association association) => value = association;

        public static implicit operator Parameter(Datum datum) => new(datum);
        public static implicit operator Parameter(Association association) => new(association);

        public static Parameter Parse(ref Parser current)
        {
            if (Datum.Parse(ref current) is Datum datum) return datum;
            if (Association.Parse(ref current) is Association association) return association;
            return null;
        }

        public Datum AsDatum => value as Datum;
        public Association AsAssociation => value as Association;

        private readonly Statement value;
    }
}