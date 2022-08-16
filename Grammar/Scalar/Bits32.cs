namespace Ronin.Grammar.Scalar;

internal class Bits32 : Datatype
{
    public Bits32() => Name = new Identifier("bits32");

    public override int ConversionDistance(Datatype datatype) => datatype switch
    {
        Anything => int.MaxValue,
        Something => int.MaxValue - 1,
        Bigint => 6,
        Bitlist => 5,
        Precise => 4,
        Number => 3,
        Bits64 => 2,
        Int64 => 1,
        Bits32 => 0,
        _ => -1
    };
}