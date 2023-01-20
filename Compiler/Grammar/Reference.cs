// Copyright © 2023 Eric Budai

using Ronin.Compiler;

using Index = Ronin.Grammar.Aggregates.Index;

namespace Ronin.Grammar;

internal class Reference : Syntax, Compiler.IParsable<Reference>
{
    public List<Value> Values { get; init; }
    public Index Index { get; init; }

    public static Reference Parse(ref Parser context)
    {
        Parser parser = context;

        var values = parser.ParseRepeating<Value>();
        if (values.Count is 0) return null;

        var index = Index.Parse(ref parser);

        return new Reference
        {
            Values = values,
            Index = index,
            Source = parser.Commit(ref context)
        };
    }
}