using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait(nameof(Parser), null)]
public class Scopes : ParsingTests
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
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var scope = Scope.Parse(ref parser);

        Assert.Single(scope.Statements);

        var datum = scope.Statements[0] as Datum;

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.Single(datum.Identifier);

        Assert.False(datum.Modifiers.Is<Compiled>());
        Assert.False(datum.Modifiers.Is<Global>());
        Assert.False(datum.Modifiers.Is<Optional>());

        var scalar = datum.Initializer as Ronin.Grammar.Literal;
        Assert.Single(scalar?.Tokens.ToArray());
    }
}