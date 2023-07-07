namespace Unit;

[Trait("Analyzer", "declare")]
public class Datum
{
    [Fact(DisplayName = "declaration")]
    public void Declaration()
    {
        Ronin.Grammar.DatumDeclaration declaration = new()
        {
            
        };
    }
}
