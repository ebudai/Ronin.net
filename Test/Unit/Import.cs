using Ronin.Compiler;

namespace Unit;

public class Import
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string line = "import standard;";

        Lexer lexer = new(line);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Statement>(syntax[0]);
        var statement = syntax[0] as Ronin.Grammar.Statement;
        var import = statement.Import;
        Assert.NotNull(import);
        Assert.NotEmpty(import.Name);
        Assert.Equal("standard", import.Name[0]);
    }

    [Fact(DisplayName = "with some hierarchy")]
    public void Hierarchy()
    {
        const string line = "import standard/funstuff/websockets;";

        Lexer lexer = new(line);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Statement>(syntax[0]);
        var statement = syntax[0] as Ronin.Grammar.Statement;
        var import = statement.Import;
        Assert.NotNull(import);
        Assert.NotEmpty(import.Name);
        Assert.Equal(3, import.Name.Length);
        Assert.Equal("standard", import.Name[0]);
        Assert.Equal("funstuff", import.Name[1]);
        Assert.Equal("websockets", import.Name[2]);
    }

    [Fact(DisplayName = "with spaces")]
    public void WithSpaces()
    {
        const string line = "import standard/ fun stuff/web sockets ;";

        Lexer lexer = new(line);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Statement>(syntax[0]);
        var statement = syntax[0] as Ronin.Grammar.Statement;
        var import = statement.Import;
        Assert.NotNull(import);
        Assert.NotEmpty(import.Name);
        Assert.Equal(3, import.Name.Length);
        Assert.Equal("standard", import.Name[0]);
        Assert.Equal("fun stuff", import.Name[1]);
        Assert.Equal("web sockets", import.Name[2]);
    }

    [Fact(DisplayName = "keywords are just text")]
    public void WithKeywords()
    {
        const string line = "import compiled to whatever/secret/stuff;";

        Lexer lexer = new(line);
        var tokens = lexer.Lex();

        // ensure hierarchy starts with a keyword
        Assert.True(tokens.Length is > 2);
        Assert.IsAssignableFrom<Ronin.Lexicon.Keyword>(tokens[2]);

        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Statement>(syntax[0]);
        var statement = syntax[0] as Ronin.Grammar.Statement;
        var import = statement.Import;
        Assert.NotNull(import);
        Assert.NotEmpty(import.Name);
        Assert.Equal(3, import.Name.Length);
        Assert.Equal("compiled to whatever", import.Name[0]);
        Assert.Equal("secret", import.Name[1]);
        Assert.Equal("stuff", import.Name[2]);
    }
}