using Ronin.Compiler;
using Ronin.Grammar.Declaration;

namespace Ronin.Grammar;

internal class Value : Syntax, IParsable<Value>
{
    public static Syntax Parse(ref Parser context)
    {
        Parser parser = context;
        var syntax = Scalar.Parse(ref parser)
            ?? Arguments.Parse(ref parser)
            ?? Name.Parse(ref parser)
            ?? Function.Parse(ref parser)
            ?? Datatype.Parse(ref parser);
        if (syntax is not Error and not null) context = parser;
        return syntax;
    }

    public static Value FromSyntax(Syntax syntax) => syntax switch
    {
        Scalar scalar => new(scalar),
        Arguments arguments => new(arguments),
        Name name => new(name),
        Function function => new(function),
        Datatype datatype => new(datatype),
        _ => null,
    };

    protected internal sealed override (int index, int length) Tokens 
        => Scalar?.Tokens 
        ?? Arguments?.Tokens 
        ?? Name?.Tokens 
        ?? Function?.Tokens 
        ?? Datatype?.Tokens 
        ?? new();

    public Scalar Scalar
    {
        get => _storage as Scalar;
        set => _storage = value;
    }

    public Arguments Arguments
    {
        get => _storage as Arguments;
        set => _storage = value;
    }

    public Name Name
    {
        get => _storage as Name; 
        set => _storage = value;
    }

    public Function Function
    {
        get => _storage as Function; 
        set => _storage = value;
    }

    public Datatype Datatype
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
