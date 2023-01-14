using Ronin.Compiler;

namespace Ronin.Grammar;

internal class Value : IParsableUnion<Value>
{
    public static Syntax Parse(ref Parser context)
        => Scalar.Parse(ref context)
        ?? Values.Parse(ref context)
        ?? Scope.Parse(ref context)
        ?? Name.Parse(ref context);

    public static implicit operator Scalar(Value value) => value._storage as Scalar;
    public static implicit operator Values(Value value) => value._storage as Values;
    public static implicit operator Scope(Value value) => value._storage as Scope;
    public static implicit operator Name(Value value) => value._storage as Name;

    public static implicit operator Value(Syntax syntax) => syntax switch
    {
        Scalar scalar => new() { _storage = scalar },
        Values arguments => new() { _storage = arguments },
        Scope scope => new() { _storage = scope },
        Name name => new() { _storage = name },
        _ => null,
    };

    private object _storage;
}
