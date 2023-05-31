using Ronin;
using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Literals;
using Ronin.Lexicon.Symbols;

namespace Unit;

[Trait("Parser", null)]
public class FunctionDeclaration
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // function test(x => number) { return 7; }

        Token[] tokens =
        {
            new Function(),
            new Word(),
            new StartValues(),
            new Word(),
            new Returns(),
            new Word(),
            new EndValues(),
            new StartScope(),
            new Word(),
            new Number(),
            new Terminal(),
            new EndScope(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var function = Ronin.Grammar.FunctionDeclaration.Parse(ref parser);

        Assert.Equal(2, function?.Identifier?.Components.Count);

        {
            Ronin.Grammar.Name name = function.Identifier.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            Ronin.Grammar.Compound.Parameters parameters = function.Identifier.Components[1];
            
            Assert.Single(parameters?.Values);
            var parameter = parameters.Values[0];
            Assert.Single(parameter?.Name?.Components);

            Assert.Single(parameter.Datatype?.Components);
            Ronin.Grammar.Name type = parameter.Datatype.Components[0];
            Assert.Equal(1, type?.Source.Length);
        }

        Assert.Single(function.Body?.Values);
        var line = function.Body.Values[0] as Ronin.Grammar.Reference;
            
        Assert.Equal(2, line?.Components?.Count);

        {
            Ronin.Grammar.Name @return = line.Components[0];
            Assert.Equal(1, @return?.Source.Length);
        }

        {
            Anonymous scalar = line.Components[1];
            Assert.Equal(1, scalar?.Source.Length);
        }
    }

    [Fact(DisplayName = "specifies return datatype")]
    public void Returns()
    {
        // function test(x => text) => optional number { return x as number; }

        Token[] tokens =
        {
            new Function(),
            new Word(),
            new StartValues(),
            new Word(),
            new Returns(),
            new Word(),
            new EndValues(),
            new Returns(),
            new Optional(),
            new Word(),
            new StartScope(),
            new Word(),
            new Word(),
            new Word(),
            new Word(),
            new Terminal(),
            new EndScope(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var function = Ronin.Grammar.FunctionDeclaration.Parse(ref parser);

        Assert.Equal(2, function?.Identifier?.Components?.Count);

        {
            Ronin.Grammar.Name name = function.Identifier.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            Ronin.Grammar.Compound.Parameters parameters = function.Identifier.Components[1];
            Assert.Single(parameters?.Values);
            var parameter = parameters.Values[0];
            Assert.Single(parameter.Name?.Components);

            Assert.Single(parameter.Datatype?.Components);
            Ronin.Grammar.Name type = parameter.Datatype.Components[0];
            Assert.Equal(1, type?.Source.Length);
        }

        Assert.Equal(1, function.Modifiers?.Source.Length);
        Assert.IsType<Optional>(function.Modifiers.Source.Span[0]);
        
        Assert.Single(function.Returns?.Components);
        Ronin.Grammar.Name returns = function.Returns.Components[0];
        Assert.Equal(1, returns?.Source.Length);

        Assert.Single(function.Body?.Values);
        var line = function.Body.Values[0] as Ronin.Grammar.Reference;
        Assert.Single(line?.Components);
        Ronin.Grammar.Name @return = line.Components[0];
        Assert.Equal(4, @return?.Source.Length);
    }
}
