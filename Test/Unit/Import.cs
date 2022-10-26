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
        Assert.IsType<Ronin.Grammar.Import>(syntax[0]);
        var import = syntax[0] as Ronin.Grammar.Import;
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
        Assert.IsType<Ronin.Grammar.Import>(syntax[0]);
        var partof = syntax[0] as Ronin.Grammar.Import;
        Assert.NotEmpty(partof.Name);
        Assert.Equal(3, partof.Name.Length);
        Assert.Equal("standard", partof.Name[0]);
        Assert.Equal("funstuff", partof.Name[1]);
        Assert.Equal("websockets", partof.Name[2]);
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
        Assert.IsType<Ronin.Grammar.Import>(syntax[0]);
        var partof = syntax[0] as Ronin.Grammar.Import;
        Assert.NotEmpty(partof.Name);
        Assert.Equal(3, partof.Name.Length);
        Assert.Equal("standard", partof.Name[0]);
        Assert.Equal("fun stuff", partof.Name[1]);
        Assert.Equal("web sockets", partof.Name[2]);
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
        Assert.IsType<Ronin.Grammar.Import>(syntax[0]);
        var partof = syntax[0] as Ronin.Grammar.Import;
        Assert.NotEmpty(partof.Name);
        Assert.Equal(3, partof.Name.Length);
        Assert.Equal("compiled to whatever", partof.Name[0]);
        Assert.Equal("secret", partof.Name[1]);
        Assert.Equal("stuff", partof.Name[2]);
    }
}