// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System.Collections.Generic;

namespace Ronin.Grammar;

/// <summary>
///     Instance of a <see cref="FunctionDeclaration"/> which can be treated as a <see cref="Datum"/>
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
    public List<Datum> Data { get; init; }
    public Context Definition { get; init; }

    public class Parameter : CompositeSyntax<Parameter, Datum.Declaration, Identifier> 
    {
        public static implicit operator Parameter(Identifier identifer) => new() { value = identifer, Source = identifer.Source };
        public static implicit operator Parameter(Datum.Declaration declaration) => new() { value = declaration, Source = declaration.Source };
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
            
            if (parser.TryAdvance<Returns>() is false) return null;

            if (Context.Parse(ref parser) is not Context definition) return null;

            return new Declaration
            {
                Parameters = parameters,
                Definition = definition,
                Source = parser.Commit(ref current)
            };
        }
    }
}
