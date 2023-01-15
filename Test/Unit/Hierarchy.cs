using Ronin.Compiler;
using Ronin.Lexicon;

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
        Assert.IsType<Ronin.Grammar.Hierarchy>(syntax[0]);
        var hierarchy = syntax[0] as Ronin.Grammar.Hierarchy;
        Assert.NotNull(hierarchy);
        Assert.NotEmpty(hierarchy.Name);
        Assert.Equal("standard", hierarchy.Name[0]);
        Assert.Equal(Ronin.Grammar.Hierarchy.Discriminator.Export, hierarchy.Direction);
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
        Assert.IsType<Ronin.Grammar.Hierarchy>(syntax[0]);
        var hierarchy = syntax[0] as Ronin.Grammar.Hierarchy;
        Assert.NotNull(hierarchy);
        Assert.NotEmpty(hierarchy.Name);
        Assert.Equal(3, hierarchy.Name.Count);
        Assert.Equal("standard", hierarchy.Name[0]);
        Assert.Equal("funstuff", hierarchy.Name[1]);
        Assert.Equal("websockets", hierarchy.Name[2]);
        Assert.Equal(Ronin.Grammar.Hierarchy.Discriminator.Import, hierarchy.Direction);
    }

    [Fact(DisplayName = "keywords are just text")]
    public void WithKeywords()
    {
        const string line = "part of compiled to whatever secret stuff;";

        Lexer lexer = new(line);
        var tokens = lexer.Lex();

        // ensure hierarchy starts with a keyword
        Assert.True(tokens.Length is > 2);
        Assert.IsAssignableFrom<Ronin.Lexicon.Keyword>(tokens[2]);

        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Hierarchy>(syntax[0]);
        var hierarchy = syntax[0] as Ronin.Grammar.Hierarchy;
        Assert.NotNull(hierarchy);
        Assert.NotEmpty(hierarchy.Name);
        Assert.Equal(5, hierarchy.Name.Count);
        Assert.Equal("compiled", hierarchy.Name[0]);
        Assert.Equal("to", hierarchy.Name[1]);
        Assert.Equal("whatever", hierarchy.Name[2]);
        Assert.Equal("secret", hierarchy.Name[3]);
        Assert.Equal("stuff", hierarchy.Name[4]);
        Assert.Equal(Ronin.Grammar.Hierarchy.Discriminator.Export, hierarchy.Direction);
    }

    [Fact(DisplayName = "using text literal")]
    public void TextLiteral()
    {
        const string line = "part of literal testing \"fast version\" readonly;";

        Lexer lexer = new(line);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        var hierarchy = syntax[0] as Ronin.Grammar.Hierarchy;
        Assert.NotNull(hierarchy);
        Assert.NotEmpty(hierarchy.Name);
        Assert.Equal(4, hierarchy.Name.Count);
        Assert.Equal("literal", hierarchy.Name[0]);
        Assert.Equal("testing", hierarchy.Name[1]);
        Assert.Equal("fast version", hierarchy.Name[2]);
        Assert.Equal("readonly", hierarchy.Name[3]);
        Assert.Equal(Ronin.Grammar.Hierarchy.Discriminator.Export, hierarchy.Direction);
    }
}