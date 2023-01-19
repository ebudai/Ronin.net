using Ronin.Compiler;

using Index = Ronin.Grammar.Aggregates.Index;

namespace Ronin.Grammar;

internal class Reference : Syntax, IParsable
{
    public List<Value> Values { get; init; }
    public Index Index { get; init; }

    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;

        List<Value> values = new();        
        var error = parser.ParseRepeating(values);
        if (error is not null) return error;
        if (values.Count is 0) return null;

        var index = Index.Parse(ref parser);
        if (index is Error) return index;

        return new Reference
        {
            Values = values,
            Index = index as Index,
            Source = parser.Commit(ref context)
        };
    }

    /*public override string ToString()
    {
        var code = string.Join(" ", Values);
        if (Index is not null) code += Index;
        return code;
    }*/
}