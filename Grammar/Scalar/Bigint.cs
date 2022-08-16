namespace Ronin.Grammar.Scalar;

public class Bigint : Datatype
{
    public Bigint() => Name = new Identifier("bigint");

    public override int ConversionDistance(Datatype datatype) => datatype switch
    {
        Anything => int.MaxValue,
        Something => int.MaxValue - 1,
        Bigint => 0,
        _ => -1
    };
}
