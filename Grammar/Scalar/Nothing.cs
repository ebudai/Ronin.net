namespace Ronin.Grammar.Scalar;

public class Nothing : Datatype
{
    public Nothing() => Name = new Identifier("nothing");

    public override int ConversionDistance(Datatype datatype) => datatype switch
    {
        Anything => int.MaxValue,
        { IsOptional: true } => 1,
        Nothing => 0,
        _ => -1
    };
}
