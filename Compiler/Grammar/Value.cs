using Ronin.Compiler;
using Ronin.Grammar.Declaration;

namespace Ronin.Grammar;

internal class Value : IParsable
{
    internal Scalar Scalar
    {
        get => _discriminator is Discriminator.Scalar ? _scalar : null;
        set
        {
            _scalar = value;
            _discriminator = Discriminator.Scalar;
        }
    }

    internal Arguments Aggregate
    {
        get => _discriminator is Discriminator.Aggregate ? _object : null;
        set
        {
            _object = value;
            _discriminator = Discriminator.Aggregate;
        }
    }

    internal Reference Reference
    {
        get => _discriminator is Discriminator.Reference ? _reference : null;
        set
        {
            _reference = value;
            _discriminator = Discriminator.Reference;
        }
    }

    internal Function Function
    {
        get => _discriminator is Discriminator.Function ? _function : null;
        set
        {
            _function = value;
            _discriminator = Discriminator.Function;
        }
    }

    internal Datatype Datatype
    {
        get => _discriminator is Discriminator.Datatype ? _datatype : null;
        set
        {
            _datatype = value;
            _discriminator = Discriminator.Datatype;
        }
    }

    public static Syntax Parse(Parser parser)
    {
        var scalar = Scalar.Parse(parser);
        if (scalar is not null) return scalar;

        var aggregate = Arguments.Parse(parser);
        if (aggregate is not null) return aggregate;

        var reference = Reference.Parse(parser);
        if (reference is not null) return reference;

        var function = Function.Parse(parser);
        if (function is not null) return function;

        return Datatype.Parse(parser);
    }

    private Value(Scalar scalar) => Scalar = scalar;
    private Value(Arguments aggregate) => Aggregate = aggregate;
    private Value(Reference reference) => Reference = reference;
    private Value(Function function) => Function = function;
    private Value(Datatype datatype) => Datatype = datatype;

    public static implicit operator Value(Syntax syntax) => syntax switch
    {
        Scalar scalar => new(scalar),
        Arguments aggregate => new(aggregate),
        Reference reference => new(reference),
        Function function => new(function),
        Datatype datatype => new(datatype),
        _ => null
    };

    public static implicit operator Value(Scalar scalar) => new(scalar);
    public static implicit operator Value(Arguments aggregate) => new(aggregate);
    public static implicit operator Value(Reference reference) => new(reference);
    public static implicit operator Value(Function function) => new(function);
    public static implicit operator Value(Datatype datatype) => new(datatype);

    public static implicit operator Scalar(Value value) => value.Scalar;
    public static implicit operator Arguments(Value value) => value.Aggregate;
    public static implicit operator Reference(Value value) => value.Reference;
    public static implicit operator Function(Value value) => value.Function;
    public static implicit operator Datatype(Value value) => value.Datatype;

    private Scalar _scalar;
    private Arguments _object;
    private Reference _reference;
    private Function _function;
    private Datatype _datatype;
    
    private Discriminator _discriminator;

    private enum Discriminator { Scalar, Aggregate, Reference, Function, Datatype };
}
