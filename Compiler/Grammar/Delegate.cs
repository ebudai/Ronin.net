// Copyright © 2023 Eric Budai

using OneOf;
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
internal class Delegate : Value.Temporary
{
    public Parameters Data { get; init; }
    public Scope Definition { get; init; }

    public class Parameter : OneOfBase<Datum, Name>, IParsable<Parameter>
    {
        protected Parameter(OneOf<Datum, Name> _) : base(_) { }

        public static implicit operator Parameter(Datum datum) => new(datum);
        public static implicit operator Parameter(Name name) => new(name);

        public static Parameter Parse(ref Parser current)
        {
            if (Datum.Parse(ref current) is Datum datum) return datum;
            if (Name.Parse(ref current) is Name name) return name;
            return null;
        }
    }

    public class Parameters : Aggregate<Parameters, OpenParenthesis, Parameter, Separator, CloseParenthesis> { }

    public static new Delegate Parse(ref Parser current)
    {
        Parser parser = current;

        if (Parameters.Parse(ref parser) is not Parameters parameters)
        {
            if (Name.Parse(ref parser) is Name name)
            {
                parameters = new Parameters { name };
            }
            else if (parser.TryAdvance<OpenParenthesis>() 
                && parser.TryAdvance<CloseParenthesis>()
                && parser.TryAdvance<Returns>())
            {
                parameters = new();
            }
            else
            {
                return null;
            }
        }

        if (Scope.Definition.Parse(ref parser) is not Scope definition) return null;

        current = parser;
        return new Delegate
        {
            Data = parameters ?? new(),
            Definition = definition
        };
    }
}
