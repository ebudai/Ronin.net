using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class Definition : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // { var test = 56; }

        List<Token> tokens = new()
        {
            StartScope(),
            Variable(),
            Word("test"),
            Assign(),
            Number(56),
            Terminal(),
            EndScope(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var scope = Ronin.Grammar.Compound.Definition.Parse(ref parser);

        Assert.Single(scope?.Values);

        var datum = scope.Values[0] as Ronin.Grammar.DatumDeclaration;

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.Single(datum.Name?.Components);

        Assert.Null(datum.Modifiers);

        var scalar = datum.Initializer as Ronin.Grammar.Literal;
        Assert.Equal(1, scalar?.Source.Length);
    }
}