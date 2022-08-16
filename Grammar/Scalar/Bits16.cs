namespace Ronin.Grammar.Scalar;

public class Bits16 : Datatype
{
    public Bits16() => Name = new Identifier("bits16");

    public override int ConversionDistance(Datatype datatype) => datatype switch
    {
        Anything => int.MaxValue,
        Something => int.MaxValue - 1,
        Bigint => 8,
        Bitlist => 7,
        Precise => 6,
        Number => 5,
        Bits64 => 4,
        Int64 => 3,
        Bits32 => 2,
        Integer => 1,
        Bits16 => 0,
        _ => -1
    };
}
