using Ronin.Compiler;

namespace Ronin.Grammar;

internal class Value : Syntax, IParsable<Value>
{
    public static Syntax Parse(ref Parser context)
        => Scalar.Parse(ref context)
        ?? Arguments.Parse(ref context)
        ?? Name.Parse(ref context);

    public static Value FromSyntax(Syntax syntax) => syntax switch
    {
        Scalar scalar => new() { _storage = scalar },
        Arguments arguments => new() { _storage = arguments },
        Name name => new() { _storage = name },
        _ => null,
    };

    public static implicit operator Scalar(Value value) => value._storage as Scalar;
    public static implicit operator Arguments(Value value) => value._storage as Arguments;
    public static implicit operator Name(Value value) => value._storage as Name;

    private object _storage;
}
