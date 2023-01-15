using Ronin.Compiler;
using Ronin.Grammar.Unions;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar.Aggregates;

internal class Scope : AggregateSyntax<Scope, OpenBrace, Statement, Terminal, CloseBrace>
{
    public Scope Parent { get; init; }
    
    public List<Datum> Data { get; } = new();
    public List<Function> Functions { get; } = new();
    public List<Datatype> Datatypes { get; } = new();

    /*public Syntax Find(Reference reference)
    {
        foreach (var data in Data) if (data.n)
    }*/

    public static Scope Global;

    static Scope()
    {
        Global = new();

    }
}
