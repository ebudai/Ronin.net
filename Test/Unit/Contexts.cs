using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class Contexts : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // { var test = 56; }

        List<Token> tokens = new()
        {
            StartScope(),
            Keyword.Variable(),
            Word("test"),
            Assign(),
            Number(56),
            Terminal(),
            EndScope(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var scope = Context.Parse(ref parser);

        Assert.Single(scope);

        var datum = scope[0] as Datum.Declaration;

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.Equal(1, datum.Identifier?.Source.Length);

        Assert.False(datum.Modifiers.Is<Compiled>());
        Assert.False(datum.Modifiers.Is<Global>());
        Assert.False(datum.Modifiers.Is<Optional>());
        Assert.False(datum.Modifiers.Is<Persistent>());

        var scalar = datum.Initializer as Ronin.Grammar.Literal;
        Assert.Equal(1, scalar?.Source.Length);
    }
}