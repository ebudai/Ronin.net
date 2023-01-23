// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;

namespace Ronin.Grammar;

/// <summary>
///     A singular piece of data residing in memory, and declared in a <see cref="Scope"/>
/// </summary>
/// <example>
///     datatype Building
///     {
///         var floors => number;
///         ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑
///     }
/// </example>
internal class Datum : Parameter, Compiler.IParsable<Datum>
{
    public Keyword Mutability { get; init; }

    public static new Datum Parse(ref Parser context)
    {
        var declarator = context.Current is Variable or Constant or Reactive ? context.Current as Keyword : null;
        if (declarator is null) return null;
        
        Parser parser = context;
        parser.Advance();

        if (Parameter.Parse(ref parser) is not Parameter parameter) return null;

        return new Datum
        {
            Mutability = declarator,
            Name = parameter.Name,
            Is = parameter.Is,
            Datatype = parameter.Datatype,
            Initializer = parameter.Initializer,
            Source = parser.Commit(ref context)
        };
    }
}