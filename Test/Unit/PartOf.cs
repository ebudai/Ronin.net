using Ronin.Compiler;
using Ronin.Lexicon;

namespace Unit;

public class PartOf
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string line = "part of standard;";

        Lexer lexer = new(line);
        Token[] tokens = lexer.Lex();
        Parser parser = new(tokens);
        /*var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Statement>(syntax[0]);
        var statement = syntax[0] as Ronin.Grammar.Statement;
        var partof = statement.PartOf;
        Assert.NotNull(partof);
        Assert.NotEmpty(partof.Name);
        Assert.Equal("standard", partof.Name[0]);*/
    }

    [Fact(DisplayName = "with some hierarchy")]
    public void Hierarchy()
    {
        const string line = "part of standard/funstuff/websockets;";

        Lexer lexer = new(line);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Statement>(syntax[0]);
        var statement = syntax[0] as Ronin.Grammar.Statement;
        var partof = statement.PartOf;
        Assert.NotNull(partof);
        Assert.NotEmpty(partof.Name);
        Assert.Equal(3, partof.Name.Length);
        Assert.Equal("standard", partof.Name[0]);
        Assert.Equal("funstuff", partof.Name[1]);
        Assert.Equal("websockets", partof.Name[2]);
    }

    [Fact(DisplayName = "with spaces")]
    public void WithSpaces()
    {
        const string line = "part of standard /fun stuff/ web sockets;";

        Lexer lexer = new(line);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Statement>(syntax[0]);
        var statement = syntax[0] as Ronin.Grammar.Statement;
        var partof = statement.PartOf;
        Assert.NotNull(partof);
        Assert.NotEmpty(partof.Name);
        Assert.Equal(3, partof.Name.Length);
        Assert.Equal("standard", partof.Name[0]);
        Assert.Equal("fun stuff", partof.Name[1]);
        Assert.Equal("web sockets", partof.Name[2]);
    }

    [Fact(DisplayName = "keywords are just text")]
    public void WithKeywords()
    {
        const string line = "part of compiled to whatever/secret/stuff;";

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
        var partof = statement.PartOf;
        Assert.NotNull(partof);
        Assert.NotEmpty(partof.Name);
        Assert.Equal(3, partof.Name.Length);
        Assert.Equal("compiled to whatever", partof.Name[0]);
        Assert.Equal("secret", partof.Name[1]);
        Assert.Equal("stuff", partof.Name[2]);
    }
}