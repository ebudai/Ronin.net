using Ronin.Compiler;
using Ronin.Grammar.Declaration;

namespace Ronin.Grammar;

internal partial class Value : Syntax, IParsable
{
    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;
        return Scalar.Parse(ref parser)
            ?? Arguments.Parse(ref parser)
            ?? Name.Parse(ref parser)
            ?? Function.Parse(ref parser)
            ?? Datatype.Parse(ref parser);
    }

    internal Scalar Scalar
    {
        get => _storage as Scalar;
        set => _storage = value;
    }

    internal Arguments Arguments
    {
        get => _storage as Arguments;
        set => _storage = value;
    }

    internal Name Name
    {
        get => _storage as Name; 
        set => _storage = value;
    }

    internal Function Function
    {
        get => _storage as Function; 
        set => _storage = value;
    }

    internal Datatype Datatype
    {
        get => _storage as Datatype; 
        set => _storage = value;        
    }

    private Value(Scalar value) => Scalar = value;
    private Value(Arguments value) => Arguments = value;
    private Value(Name value) => Name = value;
    private Value(Function value) => Function = value;
    private Value(Datatype value) => Datatype = value;

    public static implicit operator Value(Scalar value) => new(value);
    public static implicit operator Value(Arguments value) => new(value);
    public static implicit operator Value(Name value) => new(value);
    public static implicit operator Value(Function value) => new(value);
    public static implicit operator Value(Datatype value) => new(value);

    public static implicit operator Scalar(Value value) => value.Scalar;
    public static implicit operator Arguments(Value value) => value.Arguments;
    public static implicit operator Name(Value value) => value.Name;
    public static implicit operator Function(Value value) => value.Function;
    public static implicit operator Datatype(Value value) => value.Datatype;

    private object _storage;
}
