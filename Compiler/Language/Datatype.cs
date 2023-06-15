using Ronin.Grammar;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Datatype : Semantic
{
    public bool IsOptional { get; set; }

    public List<Datatype> Bases { get; } = new();
    public Context Definition { get; }

    public Datatype(DatatypeDeclaration datatype, Context context) : base(datatype)
    {
        Bases.Add(new UnresolvedDatatype(datatype.Algebra, context));
        Definition = new(datatype.Definition, context);
    }

    public class Constructed
    {
        public List<Result> Parameters { get; init; } = new();
    }

    protected internal Datatype(Reference reference) : base(reference) { }
}

[ExcludeFromCodeCoverage]
internal class UnresolvedDatatype : Datatype
{
    public Reference Reference { get; init; }
    public Context Context { get; init; }

    public UnresolvedDatatype(Reference reference, Context context) : base(reference)
    {
        Reference = reference;
        Context = context;
    }
}