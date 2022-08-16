namespace Ronin.Grammar.Scalar;

public class Integer : Datatype
{
    public Integer() => Name = new Identifier("integer");

    public override int ConversionDistance(Datatype datatype) => datatype switch
    {
        Anything => int.MaxValue,
        Something => int.MaxValue - 1,
        Bigint => 4,
        Precise => 3,
        Number => 2,
        Int64 => 1,
        Integer => 0,
        _ => -1
    };
}
