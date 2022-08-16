namespace Ronin.Grammar.Scalar;

public class Anything : Datatype
{
    public Anything() => Name = new Identifier("anything");

    public override int ConversionDistance(Datatype datatype) => datatype switch
    {
        Anything => int.MaxValue,
        Something => int.MaxValue - 1,
        _ => 1
    };
}
