// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

/// <summary>
///     Instance of a <see cref="FunctionDeclarationSyntax"/> which can be treated as a <see cref="DatumDeclarationSyntax"/>
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
internal class DelegateSyntax : Syntax, Compiler.IParsable<DelegateSyntax>
{
    public List<DatumDeclarationSyntax> Data { get; init; }
    public Scope Body { get; init; }

    public static DelegateSyntax Parse(ref Parser context)
    {
        Parser parser = context;

        List<DatumDeclarationSyntax> data;
        var datum = DatumDeclarationSyntax.Parse(ref parser);
        if (datum is null)
        {
            var parameters = Parameters.Parse(ref parser);
            data = parameters?.Values;
            if (data is not null && parser.FailsToConsume<Returns>()) return null;
        }
        else
        {
            data = new List<DatumDeclarationSyntax> { datum };
            if (parser.PreviousToken is not Returns) return null;
        }

        if (Scope.Parse(ref parser) is not Scope body) return null;

        return new DelegateSyntax
        {
            Data = data,
            Body = body,
            Source = parser.Commit(ref context)
        };
    }
}
