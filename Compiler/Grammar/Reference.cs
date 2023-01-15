using Ronin.Compiler;
using Ronin.Grammar.Unions;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;
using Index = Ronin.Grammar.Aggregates.Index;

namespace Ronin.Grammar;

internal class Reference : Syntax, IParsable
{
    public List<Value> Values { get; private init; }
    public Index Index { get; private init; }

    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;
        List<Value> values = new();

        while (parser.IsNotFinished)
        {
            if (parser.Current is Punctuation and not OpenParenthesis) break;
            
            var syntax = Value.Parse(ref parser);
            if (syntax is Error or null) return syntax;
            values.Add(syntax);
        }

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

    /*public List<IdentifierComponent> Match(IdentifierComponent[] components)
    {
        List<IdentifierComponent> misses = new();
        foreach (var value in Values)
        {
            foreach (var component in components)
            {
                Name name = component;
                if (name is not null)
                {

                }
            }
        }
        return misses;
    }*/
}