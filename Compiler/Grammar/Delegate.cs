// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;
using System.Collections.Generic;
using System.Linq;

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
internal class Delegate : AnonymousValue, IParsableSyntax<Delegate>
{
    public List<Datum.Declaration> Data { get; init; }
    public Context Definition { get; init; }

    public new static Delegate Parse(ref Parser current)
    {
        Parser parser = current;

        var data = Parameters.Parse(ref parser);        
        if (data is null)
        {
            if (Datum.Declaration.Parse(ref parser) is not Datum.Declaration datum) return null;
            if (parser.PreviousToken is not Returns) return null;
            data = new() { datum };
        }
        else
        {
            if (parser.TryAdvance<Returns>() is false) return null;
        }

        if (Context.Parse(ref parser) is not Context definition) return null;

        return new Delegate
        {
            Data = new(data),
            Definition = definition,
            Source = parser.Commit(ref current)
        };
    }
}
