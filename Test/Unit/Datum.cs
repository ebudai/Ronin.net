using Xunit;

namespace Unit;

[Trait("Analyzer", "Declaration")]
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
