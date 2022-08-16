namespace Ronin.Grammar.Scalar;

public class Int8 : Datatype
{
    public Int8() => Name = new Identifier("int8");

    public override int ConversionDistance(Datatype datatype) => datatype switch
    {
        Anything => int.MaxValue,
        Something => int.MaxValue - 1,
        Bigint => 6,
        Precise => 5,
        Number => 4,
        Int64 => 3,
        Integer => 2,
        Int16 => 1,
        Int8 => 0,
        _ => -1
    };
}
