using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
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
            switch (statement.value)
            {
                case Hierarchy: return new DatatypeCannotJoinNamedScope();
                case Assignment: return new DatatypeDefinitionCannotContain<Assignment>();
                //case Grammar.Function function: datatype.Operations.Add(function); break;
                case Scope: return new DatatypeDefinitionCannotContain<Scope>();

            }
        }        

        return datatype;
    }
}

[ExcludeFromCodeCoverage]
internal class UnresolvedDatatype : Datatype
{
    public Reference Reference { get; init; }
    public Reference Algebra { get; init; }
}

internal class DatatypeCannotJoinNamedScope : Datatype, IError
{

}

internal class DatatypeDefinitionCannotContain<T> : Datatype, IError where T : Syntax
{

}