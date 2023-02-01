using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;

namespace Unit;

[Trait("Parser", null)]
public class Hierarchy
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string line = "part of standard;";

        Lexer lexer = new(line);
        Token[] tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Ronin.Grammar.Hierarchy hierarchy = syntax[0] as Statement;
        Assert.NotNull(hierarchy);
        Assert.NotNull(hierarchy.Name);
        //Assert.IsType<Ronin.Grammar.Name>
        Assert.NotEmpty(hierarchy.Name.Words);
        Assert.Equal("standard", hierarchy.Name.Words[0]);
        Assert.IsType<PartOf>(hierarchy.Direction);
    }

    [Fact(DisplayName = "with some hierarchy")]
    public void WithHierarchy()
    {
        const string line = "import standard funstuff websockets;";

        Lexer lexer = new(line);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Ronin.Grammar.Hierarchy hierarchy = syntax[0] as Statement;
        Assert.NotNull(hierarchy);
        Assert.NotEmpty(hierarchy.Name.Words);
        Assert.Equal(3, hierarchy.Name.Words.Count);
        Assert.Equal("standard", hierarchy.Name.Words[0]);
        Assert.Equal("funstuff", hierarchy.Name.Words[1]);
        Assert.Equal("websockets", hierarchy.Name.Words[2]);
        Assert.IsType<Import>(hierarchy.Direction);
    }

    [Fact(DisplayName = "keywords are just text")]
    public void WithKeywords()
    {
        const string line = "part of thing compiled to whatever secret stuff;";

        Lexer lexer = new(line);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var statements = parser.Parse();

        Assert.NotEmpty(statements);
        Ronin.Grammar.Hierarchy hierarchy = statements[0];
        Assert.NotNull(hierarchy);
        Assert.NotEmpty(hierarchy.Name.Words);
        Assert.Equal(6, hierarchy.Name.Words.Count);
        Assert.Equal("thing", hierarchy.Name.Words[0]);
        Assert.Equal("compiled", hierarchy.Name.Words[1]);
        Assert.Equal("to", hierarchy.Name.Words[2]);
        Assert.Equal("whatever", hierarchy.Name.Words[3]);
        Assert.Equal("secret", hierarchy.Name.Words[4]);
        Assert.Equal("stuff", hierarchy.Name.Words[5]);
        Assert.IsType<PartOf>(hierarchy.Direction);
    }

    [Fact(DisplayName = "using text literal")]
    public void TextLiteral()
    {
        const string line = "part of literal testing \"fast version\" readonly;";

        Lexer lexer = new(line);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var statements = parser.Parse();

        Assert.NotEmpty(statements);
        Ronin.Grammar.Hierarchy hierarchy = statements[0];
        Assert.NotNull(hierarchy);
        Assert.NotEmpty(hierarchy.Name.Words);
        Assert.Equal(4, hierarchy.Name.Words.Count);
        Assert.Equal("literal", hierarchy.Name.Words[0]);
        Assert.Equal("testing", hierarchy.Name.Words[1]);
        Assert.Equal("fast version", hierarchy.Name.Words[2]);
        Assert.Equal("readonly", hierarchy.Name.Words[3]);
        Assert.IsType<PartOf>(hierarchy.Direction);
    }
}