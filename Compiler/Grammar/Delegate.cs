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
///     var lambda = { return x; };
///                  ↑↑↑↑↑↑↑↑↑↑↑↑↑
/// </example>
internal class Delegate : Syntax, IParsableSyntax<Delegate>
{
    public List<Datum> Data { get; init; }
    public Scope Body { get; init; }

    public static Delegate Parse(ref Parser current)
    {
        Parser parser = current;

        List<Datum> data;
        var datum = Datum.Parse(ref parser);
        if (datum is null)
        {
            var parameters = Parameters.Parse(ref parser);
            data = parameters?.Values;
            if (data is not null && parser.TryConsume<Returns>() is false) return null;
        }
        else
        {
            data = new List<Datum> { datum };
            if (parser.PreviousToken is not Returns) return null;
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
