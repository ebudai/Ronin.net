using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Failure;

[Trait("Parser", null)]
public class Assignments : ParsingTests
{
    [Fact(DisplayName = "no value")]
    public void NoValue()
    {
        // thing = ;

        List<Token> tokens = new()
        {
            Word("thing"),
            Assign(),
            Terminal(),
        };
        
        Parser parser = new(tokens);
        var assignment = Comparison.Parse(ref parser);
        
        Assert.Null(assignment);
    }

    [Fact(DisplayName = "not an assignment")]
    public void NotAnAssignment()
    {
        // what (thing) doing ?;

        List<Token> tokens = new()
        {
            Word("what"),
            StartValues(),
            Word("thing"),
            EndValues(),
            Word("doing"),
            Symbol("?"),
            Terminal(),
        };

        Parser parser = new(tokens);
        var assignment = Comparison.Parse(ref parser);

        Assert.Null(assignment);
    }

    [Fact(DisplayName = "empty")]
    public void Blank()
    {
        List<Token> tokens = new() { Sentinel.Instance };

        Parser parser = new(tokens);
        var assignment = Comparison.Parse(ref parser);

        Assert.Null(assignment);
    }
}
