// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Lexicon;

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

        List<Datum.Declaration> data;

        var parameters = Parameters.Parse(ref parser);        
        if (parameters is null)
        {
            if (Datum.Declaration.Parse(ref parser) is not Datum.Declaration datum) return null;
            if (parser.PreviousToken is not Returns) return null;
            data = new() { datum };
        }
        else
        {
            data = parameters.Values;
            if (parser.TryAdvance<Returns>() is false) return null;
        }

        if (Context.Parse(ref parser) is not Context definition) return null;

        return new Delegate
        {
            Data = data,
            Definition = definition,
            Source = parser.Commit(ref current)
        };
    }
}
