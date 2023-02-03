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
public class function
{
    [Fact(DisplayName = "bad name")]
    public void BadName()
    {
        Tokens tokens = new();
        tokens.Add<Function>()
            .Add<Word>("test")
            .Add<CloseParenthesis>()
            .Add<Word>("thing")
            .Add<OpenParenthesis>()
            .Add<Word>("x")
            .Add<Returns>()
            .Add<Word>("number")
            .Add<CloseParenthesis>()
            .Add<OpenBrace>()
            .Add<CloseBrace>();

        Parser parser = new(tokens.ToArray());
        Ronin.Grammar.Function.Parse(ref parser);

        Assert.NotEmpty(parser.Errors);
        Assert.IsType<UnexpectedSyntaxError>(parser.Errors[0]);
    }

    [Fact(DisplayName = "no identifier")]
    public void NoIdentifier()
    {
        Tokens tokens = new();
        tokens.Add<Function>()
            .Add<OpenBrace>()
            .Add<CloseBrace>();

        Parser parser = new(tokens.ToArray());
        Ronin.Grammar.Function.Parse(ref parser);

        Assert.NotEmpty(parser.Errors);
        Assert.IsType<UnexpectedSyntaxError>(parser.Errors[0]);
    }
}
