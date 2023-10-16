// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
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
                parameters = new Parameters { name };
            }
            else if (parser.TryAdvance<Open.Parenthesis>() && parser.TryAdvance<Close.Parenthesis>())
            {
                parameters = new();
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

    public override void ResolveTypes(Scope context)
    {
        Data.ResolveTypes(context);
        Definition.ResolveTypes(context);
    }

    public override void ResolveFunctions(Scope context)
    {
        Data.ResolveFunctions(context);
        Definition.ResolveFunctions(context);
    }

    public override void ResolveData(Scope context)
    {
        Data.ResolveData(context);
        Definition.ResolveData(context);
    }

    public class Parameter : IParsable<Parameter>
    {
        private Parameter(Datum datum) => value = datum;
        private Parameter(Name name) => value = name;

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
        public override void ResolveTypes(Scope context)
        {
            foreach (var parameter in this)
            {
                parameter.AsDatum?.ResolveTypes(context);
            }
        }

        public override void ResolveFunctions(Scope context)
        {
            foreach (var parameter in this)
            {
                parameter.AsDatum?.ResolveFunctions(context);
            }
        }

        public override void ResolveData(Scope context)
        {
            for (int i = 0; i != Count; ++i)
            {
                if (this[i].AsDatum is not Datum.Unresolved unresolved) continue;

                var member = context.Find(unresolved.Reference);
                this[i] = member as Datum ?? new Datum.Calculated { Member = member };
            }
        }
    }
}