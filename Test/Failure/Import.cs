using Ronin.Compiler;
using static Ronin.Grammar.Syntax.Result;

namespace Failure;

public class Import
{
    [Fact(DisplayName = "not a part of")]
    public void Basic()
    {
        const string somethingelse = "not an import;";

        Lexer lexer = new(somethingelse);
        var tokens = lexer.Lex();

        Assert.NotNull(tokens);
        Assert.NotEmpty(tokens);
        Ronin.Grammar.Import import = new();
        Assert.Equal(DoesNotApply, import.Add(tokens.Peek()));        
    }

    [Fact(DisplayName = "keyword not part of")]
    public void KeywordButNotPartOf()
    {
        const string somethingelse = "return not an import;";

        Lexer lexer = new(somethingelse);
        var tokens = lexer.Lex();

        Assert.NotNull(tokens);
        Assert.NotEmpty(tokens);
        Ronin.Grammar.Import import = new();
        Assert.Equal(DoesNotApply, import.Add(tokens.Peek()));
    }

    [Fact(DisplayName = "no non-terminal symbols allowed")]
    public void NoSymbols()
    {
        const string somethingelse = "import illegal ,things;";

        Lexer lexer = new(somethingelse);
        var tokens = lexer.Lex();

        Assert.NotNull(tokens);
        Assert.NotEmpty(tokens);
        Ronin.Grammar.Import import = new();
        var result = import.Add(tokens.Dequeue());
        Assert.Equal(Applied, result);
        while (tokens.Count is > 1)
        {
            var token = tokens.Dequeue();
            result = import.Add(token);
            var expected = token switch
            {
                Ronin.Token.Name or Ronin.Token.Whitespace => Applied,
                Ronin.Token.Symbol symbol => symbol switch
                {
                    { IsTerminal: true } => Applied,
                    _ => DoesNotApply,
                },
                _ => DoesNotApply,
            };
            Assert.Equal(expected, result);
        }
    }

    [Fact(DisplayName = "can't start with a symbol")]
    public void NoStartWithSymbol()
    {
        const string symbols = ",";

        Lexer lexer = new(symbols);
        var tokens = lexer.Lex();

        Assert.NotNull(tokens);
        Assert.NotEmpty(tokens);
        
        Ronin.Grammar.Import import = new();

        var result = import.Add(tokens.Dequeue());

        Assert.Equal(DoesNotApply, result);
    }

    [Fact(DisplayName = "can't start with a literal")]
    public void NoStartWithLiteral()
    {
        const string symbols = "0b10010";

        Lexer lexer = new(symbols);
        var tokens = lexer.Lex();

        Assert.NotNull(tokens);
        Assert.NotEmpty(tokens);

        Ronin.Grammar.Import import = new();

        var result = import.Add(tokens.Dequeue());

        Assert.Equal(DoesNotApply, result);
    }

    [Fact(DisplayName = "can't have multiple urls")]
    public void NoMultipleURLs()
    {
        const string symbols = "import git://github.com/ebudai/ronin.git git://gitlab.com/ebudai/ronin.git";

        Lexer lexer = new(symbols);
        var tokens = lexer.Lex();

        Assert.NotNull(tokens);
        Assert.NotEmpty(tokens);

        Ronin.Grammar.Import import = new();

        // import
        var result = import.Add(tokens.Dequeue());
        Assert.Equal(Applied, result);

        // whitespace
        result = import.Add(tokens.Dequeue());
        Assert.Equal(Applied, result);

        // first url literal
        result = import.Add(tokens.Dequeue());
        Assert.Equal(Applied, result);

        // whitespace
        result = import.Add(tokens.Dequeue());
        Assert.Equal(Applied, result);

        // second url literal
        Assert.Throws<Parser.Exception>(() => import.Add(tokens.Dequeue()));
    }
}
