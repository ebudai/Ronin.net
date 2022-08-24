namespace Ronin.Grammar;

public abstract class Datatype : Scope, IIdentifiable
{
    public bool IsOptional { get; set; }

    public abstract int ConversionDistance(Datatype datatype);
}
