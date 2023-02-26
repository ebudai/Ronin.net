using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;

namespace Unit;

[Trait("Parser", null)]
public class Parameters
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // (var test => money)

        Token[] tokens = 
        {
            new OpenParenthesisSymbol(),
            new VariableKeyword(),
            new Word(),
            new ReturnsSymbol(),
            new Word(),
            new CloseParenthesisSymbol(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var parameters = Ronin.Grammar.Aggregates.Parameters.Parse(ref parser);

        Assert.Single(parameters?.Values);

        DatumDeclarationSyntax datum = parameters.Values[0];

        Assert.IsType<VariableKeyword>(datum?.Mutability);

        Assert.Null(datum.Is);

        Assert.Single(datum.Name?.Source);

        Ronin.Grammar.Name name = datum?.Datatype?.Components?[0];
        Assert.Single(name?.Source);
    }

    [Fact(DisplayName = "multiple")]
    public void Multiple()
    {
        // (test => number, stuff in things => text)

        Token[] tokens = 
        {
            new OpenParenthesisSymbol(),
            new Word(),
            new ReturnsSymbol(),
            new Word(),
            new SeparatorSymbol(),
            new Word(),
            new Word(),
            new Word(),
            new ReturnsSymbol(),
            new Word(),
            new CloseParenthesisSymbol(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var parameters = Ronin.Grammar.Aggregates.Parameters.Parse(ref parser);

        Assert.Equal(2, parameters?.Values?.Count);

        {
            DatumDeclarationSyntax datum = parameters.Values[0];
            
            Assert.Null(datum?.Mutability);

            Assert.Null(datum.Is);

            Assert.Single(datum.Name?.Source);
        
            Assert.Single(datum.Datatype?.Components);
            Ronin.Grammar.Name name = datum.Datatype.Components[0];
            Assert.Single(name?.Source);
        }

        {
            DatumDeclarationSyntax datum = parameters.Values[1];

            Assert.Null(datum?.Mutability);

            Assert.Null(datum.Is);

            Assert.Single(datum.Datatype?.Components);
            Ronin.Grammar.Name name = datum.Datatype.Components[0];
            Assert.Single(name?.Source);
        }
    }

    [Fact(DisplayName = "empty parenthesis")]
    public void Empty()
    {
        // ()

        Token[] tokens = 
        {
            new OpenParenthesisSymbol(),
            new CloseParenthesisSymbol(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Aggregates.Parameters.Parse(ref parser);

        Assert.Empty(arguments?.Values);
    }
}
