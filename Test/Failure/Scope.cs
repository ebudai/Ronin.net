using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
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
        var syntax = Scope.Parse(ref parser);
        Assert.Null(syntax);
    }
}
