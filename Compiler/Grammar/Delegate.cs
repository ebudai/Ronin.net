// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Compound;
using Ronin.Lexicon.Punctuation;

namespace Ronin.Grammar;

/// <summary>
///     Instance of a <see cref="Function"/> which can be treated as a <see cref="Datum"/>
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
internal class Delegate : Anonymous, IParsableSyntax<Delegate>
{
    public List<Datum> Data { get; init; }
    public Scope Body { get; init; }

    public new static Delegate Parse(ref Parser current)
    {
        Parser parser = current;

        List<Datum> data;

        var parameters = Parameters.Parse(ref parser);        
        if (parameters is null)
        {
            var datum = Datum.Parse(ref parser);
            if (datum is null) return null;
            if (parser.PreviousToken is not Returns) return null;
            data = new() { datum };
        }
        else
        {
            data = parameters.Values;
            if (parser.TryConsume<Returns>() is false) return null;
        }

        if (Scope.Parse(ref parser) is not Scope body) return null;

        return new Delegate
        {
            Data = data,
            Body = body,
            Source = parser.Commit(ref current)
        };
    }
}
