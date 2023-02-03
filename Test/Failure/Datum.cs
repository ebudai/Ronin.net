using Ronin.Compiler;
using Ronin.Grammar.Errors;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;
using Test;

namespace Failure;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class datum
{
    [Fact(DisplayName = $"{Reactive.keyword} before name")]
    public void ReturnsBeforeName()
    {
        Tokens tokens = new();
        tokens.Add<Reactive>()
            .Add<Returns>()
            .Add<Number>("44.3")
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        Ronin.Grammar.Datum.Parse(ref parser);

        Assert.NotEmpty(parser.Errors);
        Assert.IsType<UnexpectedSyntaxError>(parser.Errors[0]);
    }

    [Fact(DisplayName = "blank datatype")]
    public void BlankDatatype()
    {
        Tokens tokens = new();
        tokens.Add<Variable>()
            .Add<Word>("x")
            .Add<Returns>()
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        Ronin.Grammar.Datum.Parse(ref parser);

        Assert.NotEmpty(parser.Errors);
        Assert.IsType<UnspecifiedDatatypeError>(parser.Errors[0]);
    }

    [Fact(DisplayName = "literal instead of identifier")]
    public void LiteralInsteadOfIdentifier()
    {
        Tokens tokens = new();
        tokens.Add<Variable>()
            .Add<Number>("555")
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        Ronin.Grammar.Datum.Parse(ref parser);

        Assert.Single(parser.Errors);
        Assert.IsType<UnexpectedSyntaxError>(parser.Errors[0]);
    }

    [Fact(DisplayName = "missing datatype and initializer")]
    public void MissingDatatypeAndInitializer()
    {
        Tokens tokens = new();
        tokens.Add<Variable>()
            .Add<Word>("x")
            .Add<Terminal>();

        Parser parser = new(tokens.ToArray());
        Ronin.Grammar.Datum.Parse(ref parser);

        Assert.NotEmpty(parser.Errors);
        Assert.IsType<UnspecifiedDatatypeError>(parser.Errors[0]);
    }
}

