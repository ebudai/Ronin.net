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
        // function test(x => number) { return 7; }

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

        Assert.Equal(2, function?.Identifier?.Components.Count);

        {
            Name name = function.Identifier.Components[0];
            Assert.Single(name?.Words);
            Assert.Equal("test", name.Words[0]);
        }

        {
            Parameters parameters = function.Identifier.Components[1];
            
            Assert.Single(parameters?.Values);
            var parameter = parameters.Values[0];
            Assert.Single(parameter?.Name?.Words);
            Assert.Equal("x", parameter.Name.Words[0]);

            Assert.Single(parameter.Datatype?.Components);
            Name type = parameter.Datatype.Components[0];
            Assert.Single(type?.Words);
            Assert.Equal("number", type.Words[0]);
        }

        Assert.Single(function.Body?.Values);
        Value value = function.Body.Values[0];
        Reference line = value;
        
        Assert.Equal(2, line?.Components?.Count);

        {
            Name @return = line.Components[0];
            Assert.Single(@return?.Words);
            Assert.Equal("return", @return.Words[0]);
        }

        {
            Scalar scalar = line.Components[1];
            Assert.Single(scalar?.Literals);
            Assert.Equal("7", scalar.Literals[0]?.ToString());
        }
    }

    [Fact(DisplayName = "specifies return datatype")]
    public void Returns()
    {
        // function test(x => text) => optional number { return x as number; }

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

        Assert.Equal(2, function?.Identifier?.Components?.Count);

        {
            Name name = function.Identifier.Components[0];
            Assert.Single(name?.Words);
            Assert.Equal("test", name.Words[0]);
        }

        {
            Parameters parameters = function.Identifier.Components[1];
            Assert.Single(parameters?.Values);
            var parameter = parameters.Values[0];
            Assert.Single(parameter.Name?.Words);
            Assert.Equal("x", parameter.Name.Words[0]);

            Assert.Single(parameter.Datatype?.Components);
            Name type = parameter.Datatype.Components[0];
            Assert.Single(type?.Words);
            Assert.Equal("text", type.Words[0]);
        }

        Assert.True(function.Modifiers?.Optional);
        Assert.False(function.Modifiers.Persistent);
        Assert.False(function.Modifiers.Shared);
        Assert.False(function.Modifiers.Compiled);

        Assert.Single(function.Returns?.Components);
        Name returns = function.Returns.Components[0];
        Assert.Single(returns?.Words);
        Assert.Equal("number", returns.Words[0]);

        Assert.Single(function.Body?.Values);
        Value value = function.Body.Values[0];
        Reference line = value;
        Assert.Single(line?.Components);
        Name @return = line.Components[0];
        Assert.Equal("return x as number", string.Join(" ", @return?.Words ?? new List<string>()));
    }
}
