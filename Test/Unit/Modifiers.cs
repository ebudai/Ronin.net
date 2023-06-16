using Ronin.Compiler;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class Modifiers : ParsingTests
{
    [Fact(DisplayName = $"{Ronin.Lexicon.Keywords.Compiled.keyword}")]
    public void IsCompiled()
    {
        // var x => compiled money;

        List<Token> tokens = new()
        {
            Variable(),
            Word("x"),
            Returns(),
            Compiled(),
            Word("money"),
            Terminal(),
        };

        Parser parser = new(tokens);
        var datum = Ronin.Grammar.DatumDeclaration.Parse(ref parser);

        Assert.True(datum.Modifiers.Is<Ronin.Lexicon.Keywords.Compiled>());
        Assert.False(datum.Modifiers.Is<Ronin.Lexicon.Keywords.Persistent>());
        Assert.False(datum.Modifiers.Is<Ronin.Lexicon.Keywords.Shared>());
        Assert.False(datum.Modifiers.Is<Ronin.Lexicon.Keywords.Optional>());
    }

    [Fact(DisplayName = $"{Ronin.Lexicon.Keywords.Persistent.keyword}")]
    public void IsPersistent()
    {
        // var x => persistent money;

        List<Token> tokens = new()
        {
            Variable(),
            Word("x"),
            Returns(),
            Persistent(),
            Word("money"),
            Terminal(),
        };

        Parser parser = new(tokens);
        var datum = Ronin.Grammar.DatumDeclaration.Parse(ref parser);

        Assert.True(datum.Modifiers.Is<Ronin.Lexicon.Keywords.Persistent>());
        Assert.False(datum.Modifiers.Is<Ronin.Lexicon.Keywords.Compiled>());
        Assert.False(datum.Modifiers.Is<Ronin.Lexicon.Keywords.Shared>());
        Assert.False(datum.Modifiers.Is<Ronin.Lexicon.Keywords.Optional>());
    }

    [Fact(DisplayName = $"{Ronin.Lexicon.Keywords.Shared.keyword}")]
    public void IsShared()
    {
        // var x => shared money;

        List<Token> tokens = new()
        {
            Variable(),
            Word("x"),
            Returns(),
            Shared(),
            Word("money"),
            Terminal(),
        };

        Parser parser = new(tokens);
        var datum = Ronin.Grammar.DatumDeclaration.Parse(ref parser);

        Assert.True(datum.Modifiers.Is<Ronin.Lexicon.Keywords.Shared>());
        Assert.False(datum.Modifiers.Is<Ronin.Lexicon.Keywords.Compiled>());
        Assert.False(datum.Modifiers.Is<Ronin.Lexicon.Keywords.Persistent>());
        Assert.False(datum.Modifiers.Is<Ronin.Lexicon.Keywords.Optional>());
    }

    [Fact(DisplayName = $"{Ronin.Lexicon.Keywords.Optional.keyword}")]
    public void IsOptional()
    {
        // var x => shared money;

        List<Token> tokens = new()
        {
            Variable(),
            Word("x"),
            Returns(),
            Optional(),
            Word("money"),
            Terminal(),
        };

        Parser parser = new(tokens);
        var datum = Ronin.Grammar.DatumDeclaration.Parse(ref parser);

        Assert.True(datum.Modifiers.Is<Ronin.Lexicon.Keywords.Optional>());
        Assert.False(datum.Modifiers.Is<Ronin.Lexicon.Keywords.Compiled>());
        Assert.False(datum.Modifiers.Is<Ronin.Lexicon.Keywords.Persistent>());
        Assert.False(datum.Modifiers.Is<Ronin.Lexicon.Keywords.Shared>());
    }
}
