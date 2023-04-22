using Ronin;
using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Lexicon.Punctuation;

namespace Failure;

[Trait("Parser", null)]
public class Arguments
{
    [Fact(DisplayName = "does not start with (")]
    public void NotAnArguments()
    {
        // not an object;

        Token[] tokens = 
        {
            new Word(),
            new Word(),
            new Word(),
            new Terminal()
        };
        
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Compound.Arguments.Parse(ref parser);

        Assert.Null(arguments);
    }

    [Fact(DisplayName = "blank")]
    public void Blank()
    {
        Token[] tokens = { Sentinel.Instance };
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Compound.Arguments.Parse(ref parser);

        Assert.Null(arguments);
    }

    [Fact(DisplayName = "bad separator")]
    public void BadSeparator()
    {
        // (test, (thing;stuff))

        Token[] tokens =
        {
            new OpenParenthesis(),
            new Word(),
            new Comma(),
            new OpenParenthesis(),
            new Word(),
            new Terminal(),
            new Word(),
            new CloseParenthesis(),
            new CloseParenthesis()
        };
        
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Compound.Arguments.Parse(ref parser);
        
        Assert.Null(arguments);
    }

    [Fact(DisplayName = "terminated incorrectly")]
    public void TerminatedWrong()
    {
        // (test;)

        Token[] tokens =
        {
            new OpenParenthesis(),
            new Word(),
            new Terminal(),
            new CloseParenthesis()
        };
        
        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Compound.Arguments.Parse(ref parser);
        
        Assert.Null(arguments);
    }
}
