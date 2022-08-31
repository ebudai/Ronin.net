namespace Ronin.Grammar.Scalar;

public class Number16 : Datatype
{
    public Number16() => Name = new Identifier("precise");

    public override int ConversionDistance(Datatype datatype) => datatype switch
    {
        Anything => int.MaxValue,
        Something => int.MaxValue - 1,
        Number64 => 2,
        Number => 1,
        Number16 => 0,
        _ => -1
    };
}

