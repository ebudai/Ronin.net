using Ronin.Grammar;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Datatype : Context
{
    public bool IsOptional { get; }

    public List<Datatype> Bases { get; } = new();
    public List<Datatype> Unions { get; } = new();
    public List<Result> Generics { get; } = new();
}

[ExcludeFromCodeCoverage]
internal class UnresolvedDatatype : Datatype
{
    public Unresolved Algebra { get; init; }
    public new List<Unresolved> Generics { get; } = new();

    public UnresolvedDatatype(DatatypeDeclaration datatype, Context context)
    {
        Context = context;
        Source = datatype;
        Algebra = new Unresolved(datatype.Algebra, context, datatype);
    }
}