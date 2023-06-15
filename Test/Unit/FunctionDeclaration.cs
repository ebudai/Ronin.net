using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class FunctionDeclaration : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // function test(x => number) { return 7; }

        List<Token> tokens = new()
        {
            Function(),
            Word("test"),
            StartValues(),
            Word("x"),
            Returns(),
            Word("number"),
            EndValues(),
            StartScope(),
            Word("return"),
            Number(7),
            Terminal(),
            EndScope(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var function = Ronin.Grammar.FunctionDeclaration.Parse(ref parser);

        Assert.Equal(2, function?.Name?.Components.Count);

        {
            Ronin.Grammar.Words name = function.Name.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            Ronin.Grammar.Compound.Parameters parameters = function.Name.Components[1];
            
            Assert.Single(parameters?.Values);
            var parameter = parameters.Values[0];
            Assert.Single(parameter?.Name?.Components);

            Assert.Single(parameter.Datatype?.Components);
            Ronin.Grammar.Words type = parameter.Datatype.Components[0];
            Assert.Equal(1, type?.Source.Length);
        }

        Assert.Single(function.Definition?.Values);
        var line = function.Definition.Values[0] as Ronin.Grammar.Reference;
            
        Assert.Equal(2, line?.Components?.Count);

        {
            Ronin.Grammar.Words @return = line.Components[0];
            Assert.Equal(1, @return?.Source.Length);
        }

        {
            Anonymous scalar = line.Components[1];
            Assert.Equal(1, scalar?.Source.Length);
        }
    }

    [Fact(DisplayName = "specifies return datatype")]
    public void ReturnsSymbol()
    {
        // function test(x => text) => number { return x as number; }

        List<Token> tokens = new()
        {
            Function(),
            Word("test"),
            StartValues(),
            Word("x"),
            Returns(),
            Word("text"),
            EndValues(),
            Returns(),
            Word("number"),
            StartScope(),
            Word("return"),
            Word("x"),
            Word("as"),
            Word("number"),
            Terminal(),
            EndScope(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var function = Ronin.Grammar.FunctionDeclaration.Parse(ref parser);

        Assert.Equal(2, function?.Name?.Components?.Count);

        {
            Ronin.Grammar.Words name = function.Name.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            Ronin.Grammar.Compound.Parameters parameters = function.Name.Components[1];
            Assert.Single(parameters?.Values);
            var parameter = parameters.Values[0];
            Assert.Single(parameter.Name?.Components);

            Assert.Single(parameter.Datatype?.Components);
            Ronin.Grammar.Words type = parameter.Datatype.Components[0];
            Assert.Equal(1, type?.Source.Length);
        }

        Assert.Single(function.Returns?.Components);
        Ronin.Grammar.Words returns = function.Returns.Components[0];
        Assert.Equal(1, returns?.Source.Length);

        Assert.Single(function.Definition?.Values);
        var line = function.Definition.Values[0] as Reference;
        Assert.Single(line?.Components);
        Ronin.Grammar.Words @return = line.Components[0];
        Assert.Equal(4, @return?.Source.Length);
    }
}
