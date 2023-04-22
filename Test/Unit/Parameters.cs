using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keyword;
using Ronin.Lexicon.Punctuation;

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
            new OpenParenthesis(),
            new Variable(),
            new Word(),
            new Returns(),
            new Word(),
            new CloseParenthesis(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var parameters = Ronin.Grammar.Compound.Parameters.Parse(ref parser);

        Assert.Single(parameters?.Values);

        Ronin.Grammar.Datum datum = parameters.Values[0];

        Assert.IsType<Variable>(datum?.Mutability);

        Assert.Null(datum.Is);

        Assert.Equal(1, datum.Name?.Source.Length);

        Ronin.Grammar.Name name = datum?.Datatype?.Components?[0];
        Assert.Equal(1, name?.Source.Length);
    }

    [Fact(DisplayName = "multiple")]
    public void Multiple()
    {
        // (test => number, stuff in things => text)

        Token[] tokens = 
        {
            new OpenParenthesis(),
            new Word(),
            new Returns(),
            new Word(),
            new Separator(),
            new Word(),
            new Word(),
            new Word(),
            new Returns(),
            new Word(),
            new CloseParenthesis(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var parameters = Ronin.Grammar.Compound.Parameters.Parse(ref parser);

        Assert.Equal(2, parameters?.Values?.Count);

        {
            Ronin.Grammar.Datum datum = parameters.Values[0];
            
            Assert.Null(datum?.Mutability);

            Assert.Null(datum.Is);

            Assert.Equal(1, datum.Name?.Source.Length);
        
            Assert.Single(datum.Datatype?.Components);
            Ronin.Grammar.Name name = datum.Datatype.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }

        {
            Ronin.Grammar.Datum datum = parameters.Values[1];

            Assert.Null(datum?.Mutability);

            Assert.Null(datum.Is);

            Assert.Single(datum.Datatype?.Components);
            Ronin.Grammar.Name name = datum.Datatype.Components[0];
            Assert.Equal(1, name?.Source.Length);
        }
    }

    [Fact(DisplayName = "empty parenthesis")]
    public void Empty()
    {
        // ()

        Token[] tokens = 
        {
            new OpenParenthesis(),
            new CloseParenthesis(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var arguments = Ronin.Grammar.Compound.Parameters.Parse(ref parser);

        Assert.Empty(arguments?.Values);
    }
}
