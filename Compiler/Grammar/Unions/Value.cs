using Ronin.Compiler;
using Ronin.Grammar.Aggregates;

namespace Ronin.Grammar.Unions;

internal class Value : IParsableUnion<Value>
{
    public static Syntax Parse(ref Parser context)
        => Scalar.Parse(ref context)
        ?? Aggregate.Parse(ref context)
        ?? Scope.Parse(ref context)
        ?? Name.Parse(ref context);

    public static implicit operator Scalar(Value value) => value._storage as Scalar;
    public static implicit operator Aggregate(Value value) => value._storage as Aggregate;
    public static implicit operator Scope(Value value) => value._storage as Scope;
    public static implicit operator Name(Value value) => value._storage as Name;

    public static implicit operator Value(Syntax syntax) => syntax switch
    {
        Scalar scalar => new() { _storage = scalar },
        Aggregate arguments => new() { _storage = arguments },
        Scope scope => new() { _storage = scope },
        Name name => new() { _storage = name },
        _ => null,
    };

    private object _storage;
}
