namespace Ronin.Grammar.Scalar;

public class Number : Datatype
{
    public Number() => Name = new Identifier("number");

    public override int ConversionDistance(Datatype datatype) => datatype switch
    {
        Anything => int.MaxValue,
        Something => int.MaxValue - 1,
        Number64 => 1,
        Number => 0,
        _ => -1
    };
}