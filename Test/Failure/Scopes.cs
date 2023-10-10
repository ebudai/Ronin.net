using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Failure;

[Trait("Parser", null)]
public class Scopes : ParsingTests
{
    [Fact(DisplayName = "missing name")]
    public void MissingName()
    {
        // { ",;,thing }

        List<Token> tokens = new()
        {
            StartScope(),
            TextDelimiter(),
            Separator(),
            Terminal(),
            Separator(),
            Word("thing"),
            EndScope(),
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var scope = Scope.Parse(ref parser);

        Assert.Null(scope);
    }

    [Fact(DisplayName = "applicative missing scope")]
    public void ApplicativeMissingScope() 
    {
        // compiled hidden;

        List<Token> tokens = new()
        {
            Keyword.Compiled(),
            Keyword.Hidden(),
            Terminal(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var scope = Scope.Parse(ref parser);

        Assert.Null(scope);
    }

    [Fact(DisplayName = "iterative missing definition")]
    public void IterativeMissingDefinition()
    {
        // iterate things => thing;

        List<Token> tokens = new()
        {
            Keyword.Iterate(),
            Word("things"),
            Returns(),
            Word("thing"),
            Terminal(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var scope = Scope.Parse(ref parser);

        Assert.Null(scope);
    }
}
