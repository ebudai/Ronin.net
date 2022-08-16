namespace Ronin.Grammar.Scalar;

public class Precise : Datatype
{
    public Precise() => Name = new Identifier("precise");

    public override int ConversionDistance(Datatype datatype) => datatype switch
    {
        Anything => int.MaxValue,
        Something => int.MaxValue - 1,
        Precise => 0,
        _ => -1
    };
}

