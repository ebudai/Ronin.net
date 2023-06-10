using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;
using Test;

namespace Failure;

[Trait("Parser", null)]
public class Ordinal : ParsingTests
{
    [Fact(DisplayName = "does not start with [")]
    public void NotAnOrdinal()
    {
        // not an ordinal;

        List<Token> tokens = new()
        {
            Word("not"),
            Word("an"),
            Word("ordinal"),
            Terminal()
        };
        
        Parser parser = new(tokens);
        var ordinal = Ronin.Grammar.Compound.Ordinal.Parse(ref parser);

        Assert.Null(ordinal);
    }

    [Fact(DisplayName = "blank")]
    public void Blank()
    {
        List<Token> tokens = new() { Sentinel.Instance };
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Compound.Ordinal.Parse(ref parser);

        Assert.Null(arguments);
    }

    [Fact(DisplayName = "bad component")]
    public void BadComponent()
    {
        // [test, (thing;stuff)]

        List<Token> tokens = new()
        {
            StartOrdinal(),
            Word("test"),
            Separator(),
            Word("thing"),
            Terminal(),
            Word("stuff"),
            EndValues(),
            EndOrdinal(),
        };
        
        Parser parser = new(tokens);
        var ordinal = Ronin.Grammar.Compound.Ordinal.Parse(ref parser);

        Assert.Null(ordinal);
    }

    [Fact(DisplayName = "terminated incorrectly")]
    public void TerminatedWrong()
    {
        // [test;]

        List<Token> tokens = new()
        {
            StartOrdinal(),
            Word("test"),
            Terminal(),
            EndOrdinal(),
        };
        
        Parser parser = new(tokens);
        var ordinal = Ronin.Grammar.Compound.Ordinal.Parse(ref parser);

        Assert.Null(ordinal);
    }
}
