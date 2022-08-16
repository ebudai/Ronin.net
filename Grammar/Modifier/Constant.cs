namespace Ronin.Grammar.Modifier;

public class Constant : Modifier
{
    public override bool AppliesToData => true;
    public override bool AppliesToDatatypes => false;
    public override bool AppliesToFunctions => true;    
}
