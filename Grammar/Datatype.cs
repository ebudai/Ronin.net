using Ronin.Grammar.Modifier;

namespace Ronin.Grammar;

public abstract class Datatype : Modifiable
{
    public Identifier Name { get; set; }

    public abstract int ConversionDistance(Datatype datatype);
}
