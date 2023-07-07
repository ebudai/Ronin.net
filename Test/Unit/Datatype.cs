using Ronin.Language;
using Test;

namespace Unit;

[Trait("Analyzer", "declare")]
public class Datatype : AnalysisTests
{
    [Fact(DisplayName = "declaration")]
    public void Declaration()
    {
        const string name = "thingy";

        // datatype thingy { }

        Ronin.Grammar.DatatypeDeclaration declaration = new()
        {
            IsExtension = false,
            Name = Name(name),
            Definition = new()
        };

        Ronin.Language.Datatype datatype = new(declaration, Context.Global);

        Assert.False(datatype.IsOptional);
        Assert.True(datatype.Definition.IsEmpty);
    }
}
