namespace Ronin.Grammar.Scalar;

public class Int64 : Datatype
{
    public Int64() => Name = new Identifier("int64");

    public override int ConversionDistance(Datatype datatype) => datatype switch
    {
        Anything => int.MaxValue,
        Something => int.MaxValue - 1,
        Bigint => 3,
        Int64 => 0,
        _ => -1
    };
}
