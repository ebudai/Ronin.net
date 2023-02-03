using Ronin.Compiler;
using Ronin.Grammar.Aggregates;
using Ronin.Grammar.Errors;
using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;
using Test;

namespace Failure;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class scope
{
    [Fact(DisplayName = "missing name")]
    public void MissingName()
    {
        Tokens tokens = new();
        tokens.Add<OpenBrace>()
            .Add<DoubleQuote>()
            .Add<Separator>()
            .Add<Terminal>()
            .Add<Separator>()
            .Add<Word>("thing")
            .Add<CloseBrace>()
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        Scope.Parse(ref parser);
        
        Assert.NotEmpty(parser.Errors);
        Assert.IsType<ExpectedSyntaxError<Terminal, CloseBrace>>(parser.Errors[0]);
    }
}
