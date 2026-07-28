using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Failure;

[Trait(nameof(Parser), null)]
public class Scopes : ParsingTests
{
    [Fact(DisplayName = "missing name")]
    public void MissingName()
    {
        // { ",;,thing }

        List<Token> tokens = new()
        {
            StartScope(),
            TextDelimiter(),
            Separator(),
            Terminal(),
            Separator(),
            Word("thing"),
            EndScope(),
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var scope = Scope.Parse(ref parser);

        Assert.Null(scope);
    }

    [Fact(DisplayName = "applicative missing scope")]
    public void ApplicativeMissingScope() 
    {
        // compiled hidden;

        List<Token> tokens = new()
        {
            Keyword.Compiled(),
            Keyword.Hidden(),
            Terminal(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var scope = Scope.Parse(ref parser);

        Assert.Null(scope);
    }

    [Fact(DisplayName = "iterative missing definition")]
    public void IterativeMissingDefinition()
    {
        // «for each thing in things;» — a header with no body is not a loop, and
        // there is no error node for it because a scope that will not parse is a
        // production declining rather than a mistake it can name.
        Lexer lexer = new("for each thing in things;\n");
        Parser parser = new(lexer.Lex());

        Assert.Null(Scope.Parse(ref parser));
    }
}
