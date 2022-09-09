using static Ronin.Grammar.Syntax.Result;

namespace Failure;

public class Import
{
    [Fact(DisplayName = "not a part of")]
    public void Basic()
    {
        const string somethingelse = "not an import;";

        Ronin.Compiler.Lexer lexer = new(somethingelse);
        var tokens = lexer.Lex();

        Assert.NotNull(tokens);
        Assert.NotEmpty(tokens);
        Ronin.Grammar.Import import = new();
        Assert.Equal(NotApplied, import.Add(tokens.Peek()));        
    }

    [Fact(DisplayName = "keyword not part of")]
    public void KeywordButNotPartOf()
    {
        const string somethingelse = "return not an import;";

        Ronin.Compiler.Lexer lexer = new(somethingelse);
        var tokens = lexer.Lex();

        Assert.NotNull(tokens);
        Assert.NotEmpty(tokens);
        Ronin.Grammar.Import import = new();
        Assert.Equal(NotApplied, import.Add(tokens.Peek()));
    }

    [Fact(DisplayName = "no non-terminal symbols allowed")]
    public void NoSymbols()
    {
        const string somethingelse = "import illegal (things);";

        Ronin.Compiler.Lexer lexer = new(somethingelse);
        var tokens = lexer.Lex();

        Assert.NotNull(tokens);
        Assert.NotEmpty(tokens);
        Ronin.Grammar.Import import = new();
        Ronin.Grammar.Syntax.Result result = import.Add(tokens.Dequeue());
        Assert.Equal(Applied, result);
        while (tokens.Count is > 1)
        {
            var token = tokens.Dequeue();
            result = import.Add(token);
            var expected = token switch
            {
                Ronin.Token.Name or Ronin.Token.Whitespace => Applied,
                Ronin.Token.Symbol symbol => symbol.IsTerminal ? Applied : NotApplied,
                _ => Error,
            };
            Assert.Equal(expected, result);
        }
    }

    [Fact(DisplayName = "can't start with a symbol")]
    public void NoStartWithSymbol()
    {
        const string symbols = ";";

        Ronin.Compiler.Lexer lexer = new(symbols);
        var tokens = lexer.Lex();

        Assert.NotNull(tokens);
        Assert.NotEmpty(tokens);
        
        Ronin.Grammar.Import import = new();

        var result = import.Add(tokens.Dequeue());

        Assert.Equal(NotApplied, result);
    }
}
