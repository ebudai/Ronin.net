// Copyright © 2023 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar.Compound;
using Ronin.Lexicon.Keyword;

namespace Ronin.Grammar;

internal class Loop : Syntax, IParsableSyntax<Loop>
{
    public Datum Header { get; init; }
    public Reference List { get; init; }
    public Scope Body { get; init; }

    public static Loop Parse(ref Parser context)
    {
        Parser parser = context;

        if (parser.FailsToConsume<ForEach>()) return null;

        if (Datum.Parse(ref parser) is not Datum header) return null;

        var list = header.Datatype is null ? null : Reference.Parse(ref parser);

        if (Scope.Parse(ref parser) is not Scope body) return null;

        return new Loop
        {
            Header = header,
            List = list,
            Body = body,
            Source = parser.Commit(ref context)
        };
    }
}
