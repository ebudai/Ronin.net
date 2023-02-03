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
public class datatype
{
    [Fact(DisplayName = "no identifier")]
    public void NoIdentifier()
    {
        Tokens tokens = new();
        tokens.Add<Datatype>()
            .Add<OpenBrace>()
            .Add<CloseBrace>()
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        Ronin.Grammar.Datatype.Parse(ref parser);

        Assert.NotEmpty(parser.Errors);
        Assert.IsType<UnexpectedSyntaxError>(parser.Errors[0]);
    }

    [Fact(DisplayName = "no scope")]
    public void NoScope()
    {
        Tokens tokens = new();
        tokens.Add<Datatype>()
            .Add<Word>("x")
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        Ronin.Grammar.Datatype.Parse(ref parser);

        Assert.NotEmpty(parser.Errors);
        Assert.IsType<ExpectedSyntaxError<OpenBrace, Assign>>(parser.Errors[0]);
    }
}
