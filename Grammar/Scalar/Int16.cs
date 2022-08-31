namespace Ronin.Grammar.Scalar;

public class Int16 : Datatype
{
    public Int16() => Name = new Identifier("int16");

    public override int ConversionDistance(Datatype datatype) => datatype switch
    {
        Anything => int.MaxValue,
        Something => int.MaxValue - 1,
        Bigint => 5,
        Number64 => 4,
        Number => 3,
        Int64 => 2,
        Integer => 1,
        Int16 => 0,
        _ => -1
    };
}
