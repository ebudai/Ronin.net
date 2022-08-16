namespace Ronin.Grammar.Modifier;

public abstract class Modifier
{
    public abstract bool AppliesToData { get; }
    public abstract bool AppliesToDatatypes { get; }    
    public abstract bool AppliesToFunctions { get; }    
}
