using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;
using Test;

using Function = Ronin.Grammar.Function;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class function
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        Tokens tokens = new();
        tokens.Add<Ronin.Lexicon.Keywords.Function>()
            .Add<Word>("test")
            .Add<OpenParenthesis>()
            .Add<Word>("x")
            .Add<Returns>()
            .Add<Word>("number")
            .Add<CloseParenthesis>()
            .Add<OpenBrace>()
            .Add<Word>("return")
            .Add<Number>("7")
            .Add<Terminal>()
            .Add<CloseBrace>();

        Parser parser = new(tokens.ToArray());
        var function = Function.Parse(ref parser);
        
        Assert.NotNull(function);

        Assert.NotNull(function.Identifier);
        Assert.Equal(2, function.Identifier.Components.Count);

        Name name = function.Identifier.Components[0];
        Assert.NotNull(name);
        Assert.Single(name.Words);
        Assert.Equal("test", name.Words[0]);

        Parameters parameters = function.Identifier.Components[1];
        Assert.NotNull(parameters);
        Assert.Single(parameters.Values);
        var parameter = parameters.Values[0];
        Assert.Single(parameter.Name.Words);
        Assert.Equal("x", parameter.Name.Words[0]);

        Assert.Null(function.Modifiers);

        Assert.Single(parameter.Datatype.Components);
        Name datatype = parameter.Datatype.Components[0];
        Assert.Single(datatype.Words);
        Assert.Equal("number", datatype.Words[0]);

        Assert.Single(function.Body.Values);
        Reference line = function.Body.Values[0];
        Assert.NotNull(line);
        Assert.Equal(2, line.Components.Count);

        Name @return = line.Components[0];
        Assert.NotNull(@return);
        Assert.Single(@return.Words);
        Assert.Equal("return", @return.Words[0]);

        Scalar scalar = line.Components[1];
        Assert.NotNull(scalar);
        Assert.Single(scalar.Literals);
        Assert.Equal("7", scalar.Literals[0].ToString());        
    }

    [Fact(DisplayName = "specifies return datatype")]
    public void Returns()
    {
        Tokens tokens = new();
        tokens.Add<Ronin.Lexicon.Keywords.Function>()
            .Add<Word>("test")
            .Add<OpenParenthesis>()
            .Add<Word>("x")
            .Add<Returns>()
            .Add<Word>("text")
            .Add<CloseParenthesis>()
            .Add<Returns>()
            .Add<Optional>()
            .Add<Word>("number")
            .Add<OpenBrace>()
            .Add<Word>("return")
            .Add<Word>("x")
            .Add<Word>("as")
            .Add<Word>("number")
            .Add<Terminal>()
            .Add<CloseBrace>();

        Parser parser = new(tokens.ToArray());
        var function = Function.Parse(ref parser);

        Assert.NotNull(function);
        Assert.Equal(2, function.Identifier.Components.Count);

        Name name = function.Identifier.Components[0];
        Assert.NotNull(name);
        Assert.Single(name.Words);
        Assert.Equal("test", name.Words[0]);

        Parameters parameters = function.Identifier.Components[1];
        Assert.NotNull(parameters);
        Assert.Single(parameters.Values);
        var parameter = parameters.Values[0];
        Assert.Single(parameter.Name.Words);
        Assert.Equal("x", parameter.Name.Words[0]);

        Assert.Single(parameter.Datatype.Components);
        Name datatype = parameter.Datatype.Components[0];
        Assert.Single(datatype.Words);
        Assert.Equal("text", datatype.Words[0]);

        Assert.True(function.Modifiers.Optional);
        Assert.False(function.Modifiers.Persistent);
        Assert.False(function.Modifiers.Shared);
        Assert.False(function.Modifiers.Compiled);

        Assert.NotNull(function.Returns);
        Assert.Single(function.Returns.Components);
        name = function.Returns.Components[0];
        Assert.Single(name.Words);        
        Assert.Equal("number", name.Words[0]);

        Assert.Single(function.Body.Values);
        Reference line = function.Body.Values[0];
        Assert.NotNull(line);
        Assert.Single(line.Components);

        Name @return = line.Components[0];
        Assert.NotNull(@return);
        Assert.Equal(4, @return.Words.Count);
        Assert.Equal("return", @return.Words[0]);
        Assert.Equal("x", @return.Words[1]);
        Assert.Equal("as", @return.Words[2]);
        Assert.Equal("number", @return.Words[3]);
    }
}
