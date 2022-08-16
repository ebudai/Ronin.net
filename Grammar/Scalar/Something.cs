namespace Ronin.Grammar.Scalar;

public class Something : Datatype
{
    public Something() => Name = new Identifier("something");

    public override int ConversionDistance(Datatype datatype) => datatype switch
    {
        Something => 0,
        Nothing => -1,
        _ => int.MaxValue
    };
}
