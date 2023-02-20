using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Aggregates;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;

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

        Token[] tokens =
        {
            new Ronin.Lexicon.Keywords.Function(),
            new Word(),
            new OpenParenthesis(),
            new Word(),
            new Returns(),
            new Word(),
            new CloseParenthesis(),
            new OpenBrace(),
            new Word(),
            new Number(),
            new Terminal(),
            new CloseBrace(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var function = Function.Parse(ref parser);

        Assert.Equal(2, function?.Identifier?.Components.Count);

        {
            Name name = function.Identifier.Components[0];
            Assert.Single(name?.Source);
        }

        {
            Parameters parameters = function.Identifier.Components[1];
            
            Assert.Single(parameters?.Values);
            var parameter = parameters.Values[0];
            Assert.Single(parameter?.Name?.Source);

            Assert.Single(parameter.Datatype?.Components);
            Name type = parameter.Datatype.Components[0];
            Assert.Single(type?.Source);
        }

        Assert.Single(function.Body?.Values);
        Value value = function.Body.Values[0];
        Reference line = value;
        
        Assert.Equal(2, line?.Components?.Count);

        {
            Name @return = line.Components[0];
            Assert.Single(@return?.Source);
        }

        {
            Scalar scalar = line.Components[1];
            Assert.Single(scalar?.Source);
        }
    }

    [Fact(DisplayName = "specifies return datatype")]
    public void Returns()
    {
        // function test(x => text) => optional number { return x as number; }

        Token[] tokens =
        {
            new Ronin.Lexicon.Keywords.Function(),
            new Word(),
            new OpenParenthesis(),
            new Word(),
            new Returns(),
            new Word(),
            new CloseParenthesis(),
            new Returns(),
            new Optional(),
            new Word(),
            new OpenBrace(),
            new Word(),
            new Word(),
            new Word(),
            new Word(),
            new Terminal(),
            new CloseBrace(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var function = Function.Parse(ref parser);

        Assert.Equal(2, function?.Identifier?.Components?.Count);

        {
            Name name = function.Identifier.Components[0];
            Assert.Single(name?.Source);
        }

        {
            Parameters parameters = function.Identifier.Components[1];
            Assert.Single(parameters?.Values);
            var parameter = parameters.Values[0];
            Assert.Single(parameter.Name?.Source);

            Assert.Single(parameter.Datatype?.Components);
            Name type = parameter.Datatype.Components[0];
            Assert.Single(type?.Source);
        }

        Assert.Single(function.Modifiers?.Source);
        Assert.IsType<Optional>(function.Modifiers.Source[0]);
        
        Assert.Single(function.Returns?.Components);
        Name returns = function.Returns.Components[0];
        Assert.Single(returns?.Source);

        Assert.Single(function.Body?.Values);
        Value value = function.Body.Values[0];
        Reference line = value;
        Assert.Single(line?.Components);
        Name @return = line.Components[0];
        Assert.Equal(4, @return?.Source.Length);
    }
}
