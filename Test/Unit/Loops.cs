using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait(nameof(Parser), null)]
public class IteratingScopes : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // iterate cars => var car { car speed = 9000; }

        List<Token> tokens = new()
        {
            Keyword.Iterate(),
            Word("cars"),
            Returns(),
            Keyword.Variable(),
            Word("car"),
            StartScope(),
            Word("car"),
            Word("speed"),
            Assign(),
            Number(9000),
            Terminal(),
            EndScope(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var loop = Scope.Iterating.Parse(ref parser);

        Assert.NotNull(loop?.Iterable);

        Assert.Single(loop.Statements);

        var assignment = loop.Statements[0] as Association;
        Assert.NotNull(assignment);
    }

    [Fact(DisplayName = "specifies datatype")]
    public void SpecifiesDatatype()
    {
        // iterate values => var value { value++; }

        List<Token> tokens = new()
        {
            Keyword.Iterate(),
            Word("values"),
            Returns(),
            Keyword.Variable(),
            Word("value"),
            StartScope(),
            Word("value"),
            Symbol("+"),
            Symbol("+"),
            Terminal(),
            EndScope(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var loop = Scope.Iterating.Parse(ref parser);

        Assert.NotNull(loop?.Iterable);
    }
}
