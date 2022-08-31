namespace Ronin.Grammar.Scalar;

public class Byte : Datatype
{
    public Byte() => Name = new Identifier("byte");

    public override int ConversionDistance(Datatype datatype) => datatype switch
    {
        Anything => int.MaxValue,
        Something => int.MaxValue - 1,
        Bigint => 10,
        Bitlist => 9,
        Number64 => 8,
        Number => 7,
        Bits64 => 6,
        Int64 => 5,
        Bits32 => 4,
        Integer => 3,
        Bits16 => 2,
        Int16 => 1,
        Byte => 0,
        _ => -1
    };
}