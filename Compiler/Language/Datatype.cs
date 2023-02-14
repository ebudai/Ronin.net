using Ronin.Compiler;
using Ronin.Grammar;

namespace Ronin.Language;

internal class Datatype
{
    public Identifier Identifier { get; init; }
    
    public List<Datatype> InnerDatatypes { get; } = new();
    public List<Datum> Data { get; } = new();
    public List<Function> Operations { get; } = new();

    public List<Datatype> Parents { get; } = new();
    public List<Datatype> Unions { get; } = new();

    public static Datatype Analyze(ref SemanticAnalyzer analyzer)
    {
        if (analyzer.CurrentSyntax is not Grammar.Datatype type) return null;

        UnresolvedDatatype datatype = new() { Identifier = type.Identifier, Algebra = type.Algebra };

        foreach (var statement in type.Body.Values)
        {
            
        }        

        return datatype;
    }
}

internal class UnresolvedDatatype : Datatype
{
    public Reference Reference { get; init; }
    public Reference Algebra { get; init; }
}