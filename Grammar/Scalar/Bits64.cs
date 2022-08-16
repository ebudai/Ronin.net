namespace Ronin.Grammar.Scalar;

public class Bits64 : Datatype
{
    public Bits64() => Name = new Identifier("bits64");

    public override int ConversionDistance(Datatype datatype) => datatype switch
    {
        Anything => int.MaxValue,
        Something => int.MaxValue - 1,
        Bigint => 2,
        Bitlist => 1,
        Bits64 => 0,
        _ => -1
    };
}
