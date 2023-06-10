using Ronin.Grammar;
using Ronin.Lexicon.Symbols;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Datatype : Context
{
    public bool IsOptional { get; }

    public List<Datatype> Bases { get; } = new();
    public List<Datatype> Unions { get; } = new();
}

[ExcludeFromCodeCoverage]
internal class UnresolvedDatatype : Datatype
{
    public Unresolved Algebra { get; init; }
    public new Context Context
    {
        get => base.Context;
        set
        {
            base.Context = value;
            Algebra.Context = value;
        }
    }

    public UnresolvedDatatype(DatatypeDeclaration datatype)
    {
        Source = datatype;
        Algebra = new Unresolved(datatype.Algebra, datatype);
    }
}