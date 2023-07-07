using Ronin.Grammar;
using Ronin.Lexicon.Keywords;

namespace Ronin.Language;

internal class Datum : Semantic
{
    public Mutability Mutability { get; init; }
    public bool IsCompiled { get; set; }
    public bool IsShared { get; set; }
    public bool IsPersistent { get; set; }
    public Modifiers Modifiers { get; init; }
    public Datatype Datatype { get; init; }
    public Result Initializer { get; init; }
    public bool Initialized { get; set; }

    public Datum(DatumDeclaration datum, Context context) : base(datum)
    {        
        Mutability = datum.Mutability switch
        {
            Variable => Mutability.Variable,
            Reactive => Mutability.Reactive,
            _ => Mutability.Constant
        };

        IsCompiled = datum.Modifiers.Is<Compiled>();
        IsShared = datum.Modifiers.Is<Shared>();
        IsPersistent = datum.Modifiers.Is<Persistent>();

        Datatype = new UnresolvedDatatype(datum.Datatype, context) { IsOptional = datum.Modifiers.Is<Optional>() };

        Initializer = new Result(datum.Initializer, context);
    }

    protected internal Datum(Reference reference) : base(reference) { }
}

internal class UnresolvedDatum : Datum
{
    public Reference Reference { get; }
    public Context Context { get; }

    public UnresolvedDatum(Reference reference, Context context) : base(reference)
    {
        Reference = reference;
        Context = context;
    }
}

internal enum Mutability { Constant, Variable, Reactive }