namespace Failure;

public class PartOf
{
    /*[Fact(DisplayName = "not a part of")]
    public void Basic()
    {
        const string somethingelse = "not a part of;";

        Ronin.Compiler.Lexer lexer = new(somethingelse);
        var tokens = lexer.Lex();
        Ronin.Compiler.Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);        
    }*/

    /*[Fact(DisplayName = "keyword not part of")]
    public void KeywordButNotPartOf()
    {
        const string somethingelse = "import not a part of;";

        Ronin.Compiler.Lexer lexer = new(somethingelse);
        var tokens = lexer.Lex();

        Assert.NotNull(tokens);
        Assert.NotEmpty(tokens);
        Ronin.Language.PartOf partof = new();
        Assert.Equal(DoesNotApply, partof.Add(tokens.Peek()));
    }

    [Fact(DisplayName = "no non-terminal symbols allowed")]
    public void NoSymbols()
    {
        const string somethingelse = "part of illegal ,things';";

        Ronin.Compiler.Lexer lexer = new(somethingelse);
        var tokens = lexer.Lex();

        Assert.NotNull(tokens);
        Assert.NotEmpty(tokens);
        Ronin.Language.PartOf partof = new();
        Ronin.Grammar.Syntax.Result result = partof.Add(tokens.Dequeue());
        Assert.Equal(Applied, result);
        while (tokens.Count is > 1)
        {
            var token = tokens.Dequeue();
            result = partof.Add(token);
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

        Ronin.Compiler.Lexer lexer = new(symbols);
        var tokens = lexer.Lex();

        Assert.NotNull(tokens);
        Assert.NotEmpty(tokens);
        
        Ronin.Language.PartOf partof = new();

        var result = partof.Add(tokens.Dequeue());

        Assert.Equal(DoesNotApply, result);
    }*/
}
