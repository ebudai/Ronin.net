// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
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
internal class Delegate : Value.Anonymous
{
    public Dictionary<Identifier, Datum> Data { get; init; }
    public Context Definition { get; init; }

    public class Parameter : UnionSyntax<Parameter, Datum.Declaration, Identifier, Comparison> 
    {
        public static implicit operator Parameter(Identifier identifer) => new() { value = identifer, Source = identifer.Source };
    }

    public class Parameters : Aggregate<Parameters, StartValues, Parameter, Separator, EndValues> { }

    public class Declaration : Anonymous, IParsableSyntax<Declaration>
    {
        public Parameters Parameters { get; init; }
        public Context Definition { get; init; }

        public new static Declaration Parse(ref Parser current)
        {
            Parser parser = current;

            var parameters = Parameters.Parse(ref parser);
            if (parameters is null)
            {
                if (Identifier.Parse(ref parser) is not Identifier identifier) return null;
                parameters = new() { identifier };
            }
            
            if (parser.TryParse<Returns>() is null) return null;

            if (Context.Parse(ref parser) is not Context definition) return null;

            return new Declaration
            {
                Parameters = parameters,
                Definition = definition,
                Source = parser.Commit(ref current)
            };
        }

        public void Define(Context context, List<Error> errors)
        {
            Definition.Define(context, errors);

            Dictionary<Identifier, Datum> data = new(Parameters.Count);
            foreach (var parameter in Parameters)
            {
                Datum.Declaration declaration = parameter;
                if (declaration?.Define(context, errors) is Datum datum)
                {
                    data.Add(declaration.Identifier, datum);
                    continue;
                }

                Identifier identifier = parameter;
                Comparison comparison = parameter;
                Syntax source = identifier ?? comparison as Syntax;
                Identifier name = identifier ?? comparison.Left;
                if (name is null)
                {
                    errors.Add(Error.UnknownSyntax(source));
                    continue;
                }
                datum = new() { Source = source.Source };
                data.Add(name, datum);
            }

            Delegate @delegate = new()
            {
                Data = data,
                Definition = Definition,
                Source = Source
            };

            context.Add(@delegate);
        }
    }
}
