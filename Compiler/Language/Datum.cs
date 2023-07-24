using Ronin.Grammar;

namespace Ronin.Language;

/*internal class Datum : Semantic
{
    

    public Datum() { }

    public Datum(Datum.Declaration datum, Context context) : base(datum)
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
}

internal class UnresolvedDatum : Datum
{
    public Reference Reference { get; }
    public Context Context { get; }

    public UnresolvedDatum(Reference reference, Context context)
    {
        Reference = reference;
        Context = context;
    }
}
*/
