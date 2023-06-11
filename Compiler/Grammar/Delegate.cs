// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Compound;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

/// <summary>
///     Instance of a <see cref="FunctionDeclaration"/> which can be treated as a <see cref="DatumDeclaration"/>
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
    public List<DatumDeclaration> Data { get; init; }
    public Definition Definition { get; init; }

    public new static Delegate Parse(scoped ref Parser current)
    {
        Parser parser = current;

        List<DatumDeclaration> data;

        var parameters = Parameters.Parse(ref parser);        
        if (parameters is null)
        {
            if (DatumDeclaration.Parse(ref parser) is not DatumDeclaration datum) return null;
            if (parser.PreviousToken is not Returns) return null;
            data = new() { datum };
        }
        else
        {
            data = parameters.Values;
            if (parser.TryAdvance<Returns>() is false) return null;
        }

        if (Definition.Parse(ref parser) is not Definition definition) return null;

        return new Delegate
        {
            Data = data,
            Definition = definition,
            Source = parser.Commit(ref current)
        };
    }
}
