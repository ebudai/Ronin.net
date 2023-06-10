using Ronin.Grammar;
using Ronin.Lexicon.Keywords;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Datum : Semantic
{
    public Mutability Mutability { get; init; }
    public Modifiers Is { get; init; }
    public Datatype Datatype { get; }
    public Result Initializer { get; init; }
}

[ExcludeFromCodeCoverage]
internal class UnresolvedDatum : Datum
{
    public Unresolved UnresolvedDatatype { get; init; }
    public Unresolved UnresolvedInitializer { get; init; }
    public new Context Context
    {
        get => base.Context;
        set
        {
            base.Context = value;
            UnresolvedDatatype.Context = value;
            UnresolvedInitializer.Context = value;
        }
    }

    public UnresolvedDatum(DatumDeclaration datum)
    {
        Mutability = datum.Mutability switch
        {
            Variable => Mutability.Variable,
            Reactive => Mutability.Reactive,
            _ => Mutability.Constant
        };

        Is = datum.Mutability switch
        {
            Compiled => Modifiers.Compiled,
            Shared => Modifiers.Shared,
            Persistent => Modifiers.Persistent,
            _ => Modifiers.None,
        };

        UnresolvedDatatype = new(datum.Datatype, datum);

        if (datum.Initializer is Reference initializer)
        {
            UnresolvedInitializer = new(initializer, datum);
        }
        else if (datum.Initializer is Anonymous value)
        {
            Initializer = value;
        }
        else
        {
            Errors.Add(new DeveloperMistakeUnhandledSubclass<Value> { Statement = datum });
        }
    }
}

internal enum Mutability { Constant, Variable, Reactive }

[Flags]
internal enum Modifiers 
{
    None        = 0,
    Compiled    = 1 << 0, 
    Persistent  = 1 << 1, 
    Shared      = 1 << 2, 
}