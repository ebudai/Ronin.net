using Ronin.Compiler;

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

    internal Aggregate Aggregate
    {
        get => _discriminator is Discriminator.Object ? _object : null;
        set
        {
            _object = value;
            _discriminator = Discriminator.Object;
        }
    }

    public static Syntax Parse(Parser parser) => Scalar.Parse(parser) ?? Aggregate.Parse(parser);

    private Value(Scalar scalar) => Scalar = scalar;
    private Value(Aggregate aggregate) => Aggregate = aggregate;

    public static implicit operator Value(Scalar scalar) => new(scalar);
    public static implicit operator Value(Aggregate aggregate) => new(aggregate);

    public static implicit operator Scalar(Value value) => value.Scalar;
    public static implicit operator Aggregate(Value value) => value.Aggregate;

    private Scalar _scalar;
    private Aggregate _object;
    private Discriminator _discriminator;

    private enum Discriminator { Scalar, Object };
}
