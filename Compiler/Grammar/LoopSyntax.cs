// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class LoopSyntax : Syntax, Compiler.IParsable<LoopSyntax>
{
    public DatumDeclarationSyntax Header { get; init; }
    public Reference List { get; init; }
    public Scope Body { get; init; }

    public static LoopSyntax Parse(ref Parser context)
    {
        Parser parser = context;

        if (parser.FailsToConsume<ForEachKeyword>()) return null;

        if (DatumDeclarationSyntax.Parse(ref parser) is not DatumDeclarationSyntax header) return null;

        var list = header.Datatype is null ? null : Reference.Parse(ref parser);

        if (Scope.Parse(ref parser) is not Scope body) return null;

        return new LoopSyntax
        {
            Header = header,
            List = list,
            Body = body,
            Source = parser.Commit(ref context)
        };
    }
}
