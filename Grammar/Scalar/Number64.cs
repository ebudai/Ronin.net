namespace Ronin.Grammar.Scalar;

public class Number64 : Datatype
{
    public Number64() => Name = new Identifier("precise");

    public override int ConversionDistance(Datatype datatype) => datatype switch
    {
        Anything => int.MaxValue,
        Something => int.MaxValue - 1,
        Number64 => 0,
        _ => -1
    };
}

