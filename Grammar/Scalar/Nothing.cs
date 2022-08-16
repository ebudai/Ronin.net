using Ronin.Grammar.Modifier;

namespace Ronin.Grammar.Scalar;

public class Nothing : Datatype
{
    public Nothing() => Name = new Identifier("nothing");

    public override int ConversionDistance(Datatype datatype) => datatype.Is<Optional>() ? 0 : -1;
}
