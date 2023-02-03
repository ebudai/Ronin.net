using Ronin.Compiler;
using Ronin.Grammar.Errors;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Symbols;
using Test;

namespace Failure;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class hierarchy
{
    [Fact(DisplayName = "missing name")]
    public void MissingName()
    {
        Tokens tokens = new();
        tokens.Add<PartOf>()
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        Ronin.Grammar.Hierarchy.Parse(ref parser);

        Assert.Single(parser.Errors);
        Assert.IsType<UnexpectedSyntaxError>(parser.Errors[0]);
    }

    [Fact(DisplayName = "improperly terminated")]
    public void Unterminated()
    {
        Tokens tokens = new();
        tokens.Add<PartOf>()
            .Add<Word>("thing")
            .Add<Slash>()
            .Add<Word>("stuff")
            .Add<OpenParenthesis>();

        Parser parser = new(tokens.ToArray());
        Ronin.Grammar.Hierarchy.Parse(ref parser);

        Assert.NotEmpty(parser.Errors);
        Assert.IsType<UnexpectedSyntaxError>(parser.Errors[0]);
    }

    [Fact(DisplayName = "bad name")]
    public void BadName()
    {
        Tokens tokens = new();
        tokens.Add<PartOf>()
            .Add<Word>("thing")
            .Add<CloseParenthesis>()
            .Add<Word>("stuff")
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        Ronin.Grammar.Hierarchy.Parse(ref parser);

        Assert.NotEmpty(parser.Errors);
        Assert.IsType<UnexpectedSyntaxError>(parser.Errors[0]);
    }
}
