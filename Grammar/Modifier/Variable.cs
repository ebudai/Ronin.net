namespace Ronin.Grammar.Modifier;

public class Variable : Modifier
{
    public override bool AppliesToData => true;
    public override bool AppliesToDatatypes => false;
    public override bool AppliesToFunctions => true;
}