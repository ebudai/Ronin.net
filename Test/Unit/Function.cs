using Ronin;
using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keyword;
using Ronin.Lexicon.Punctuation;

namespace Unit;

[Trait("Parser", null)]
public class Function
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // function test(x => number) { return 7; }

        Token[] tokens =
        {
            new Ronin.Lexicon.Keyword.Function(),
            new Word(),
            new OpenParenthesis(),
            new Word(),
            new Returns(),
            new Word(),
            new CloseParenthesis(),
            new OpenBrace(),
            new Word(),
            new NumberLiteral(),
            new Terminal(),
            new CloseBrace(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var function = Ronin.Grammar.Function.Parse(ref parser);

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
            new Ronin.Lexicon.Keyword.Function(),
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
        var function = Ronin.Grammar.Function.Parse(ref parser);

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
