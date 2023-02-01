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
        Ronin.Grammar.Hierarchy hierarchy = syntax[0];
        Assert.NotNull(hierarchy);

        Assert.IsType<PartOf>(hierarchy.Direction);

        Assert.Single(hierarchy.Components);
        Ronin.Grammar.Name name = hierarchy.Components[0];
        Assert.NotEmpty(name.Words);
        Assert.Equal("standard", name.Words[0]);        
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
        Ronin.Grammar.Hierarchy hierarchy = syntax[0];
        Assert.NotNull(hierarchy);

        Assert.IsType<Import>(hierarchy.Direction);

        Assert.Single(hierarchy.Components);
        Ronin.Grammar.Name name = hierarchy.Components[0];
        Assert.Equal(3, name.Words.Count);
        Assert.Equal("standard", name.Words[0]);
        Assert.Equal("funstuff", name.Words[1]);
        Assert.Equal("websockets", name.Words[2]);        
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

        Assert.IsType<PartOf>(hierarchy.Direction);

        Assert.Single(hierarchy.Components);
        Ronin.Grammar.Name name = hierarchy.Components[0];
        Assert.Equal(6, name.Words.Count);
        Assert.Equal("thing", name.Words[0]);
        Assert.Equal("compiled", name.Words[1]);
        Assert.Equal("to", name.Words[2]);
        Assert.Equal("whatever", name.Words[3]);
        Assert.Equal("secret", name.Words[4]);
        Assert.Equal("stuff", name.Words[5]);        
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

        Assert.IsType<PartOf>(hierarchy.Direction);

        Assert.Equal(3, hierarchy.Components.Count);

        Ronin.Grammar.Name name = hierarchy.Components[0];
        Assert.Equal(2, name.Words.Count);
        Assert.Equal("literal", name.Words[0]);
        Assert.Equal("testing", name.Words[1]);

        Ronin.Grammar.Scalar scalar = hierarchy.Components[1];
        Assert.Single(scalar.Literals);
        Assert.Equal("\"fast version\"", scalar.Literals[0].ToString());

        name = hierarchy.Components[2];
        Assert.Single(name.Words);
        Assert.Equal("readonly", name.Words[0]);
    }
}