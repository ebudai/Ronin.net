namespace Ronin.Grammar.Modifier;

public class Optional : Modifier
{
    public override bool AppliesToData => true;
    public override bool AppliesToDatatypes => false;
    public override bool AppliesToFunctions => false;
}
