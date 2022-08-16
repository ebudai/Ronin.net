namespace Ronin.Grammar.Scalar;

public class Bitlist : Datatype
{
    public Bitlist() => Name = new Identifier("bitlist");

    public override int ConversionDistance(Datatype datatype) => datatype switch
    {
        Anything => int.MaxValue,
        Something => int.MaxValue - 1,
        Bitlist => 0,
        _ => -1
    };
}
