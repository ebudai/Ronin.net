using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;

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
            new FunctionKeyword(),
            new Word(),
            new OpenParenthesisSymbol(),
            new Word(),
            new ReturnsSymbol(),
            new Word(),
            new CloseParenthesisSymbol(),
            new OpenBraceSymbol(),
            new Word(),
            new NumberLiteral(),
            new TerminalSymbol(),
            new CloseBraceSymbol(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var function = FunctionDeclarationSyntax.Parse(ref parser);

        Assert.Equal(2, function?.Identifier?.Components.Count);

        {
            Ronin.Grammar.Name name = function.Identifier.Components[0];
            Assert.Single(name?.Source);
        }

        {
            Ronin.Grammar.Aggregates.Parameters parameters = function.Identifier.Components[1];
            
            Assert.Single(parameters?.Values);
            var parameter = parameters.Values[0];
            Assert.Single(parameter?.Name?.Source);

            Assert.Single(parameter.Datatype?.Components);
            Ronin.Grammar.Name type = parameter.Datatype.Components[0];
            Assert.Single(type?.Source);
        }

        Assert.Single(function.Body?.Values);
        Value value = function.Body.Values[0];
        Ronin.Grammar.Reference line = value;
        
        Assert.Equal(2, line?.Components?.Count);

        {
            Ronin.Grammar.Name @return = line.Components[0];
            Assert.Single(@return?.Source);
        }

        {
            LiteralSyntax scalar = line.Components[1];
            Assert.Single(scalar?.Source);
        }
    }

    [Fact(DisplayName = "specifies return datatype")]
    public void Returns()
    {
        // function test(x => text) => optional number { return x as number; }

        Token[] tokens =
        {
            new FunctionKeyword(),
            new Word(),
            new OpenParenthesisSymbol(),
            new Word(),
            new ReturnsSymbol(),
            new Word(),
            new CloseParenthesisSymbol(),
            new ReturnsSymbol(),
            new OptionalKeyword(),
            new Word(),
            new OpenBraceSymbol(),
            new Word(),
            new Word(),
            new Word(),
            new Word(),
            new TerminalSymbol(),
            new CloseBraceSymbol(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var function = FunctionDeclarationSyntax.Parse(ref parser);

        Assert.Equal(2, function?.Identifier?.Components?.Count);

        {
            Ronin.Grammar.Name name = function.Identifier.Components[0];
            Assert.Single(name?.Source);
        }

        {
            Ronin.Grammar.Aggregates.Parameters parameters = function.Identifier.Components[1];
            Assert.Single(parameters?.Values);
            var parameter = parameters.Values[0];
            Assert.Single(parameter.Name?.Source);

            Assert.Single(parameter.Datatype?.Components);
            Ronin.Grammar.Name type = parameter.Datatype.Components[0];
            Assert.Single(type?.Source);
        }

        Assert.Single(function.Modifiers?.Source);
        Assert.IsType<OptionalKeyword>(function.Modifiers.Source[0]);
        
        Assert.Single(function.Returns?.Components);
        Ronin.Grammar.Name returns = function.Returns.Components[0];
        Assert.Single(returns?.Source);

        Assert.Single(function.Body?.Values);
        Value value = function.Body.Values[0];
        Ronin.Grammar.Reference line = value;
        Assert.Single(line?.Components);
        Ronin.Grammar.Name @return = line.Components[0];
        Assert.Equal(4, @return?.Source.Length);
    }
}
