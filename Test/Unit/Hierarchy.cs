using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;

namespace Unit;

[Trait("Parser", null)]
public class Hierarchy
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // part of things;

        Token[] tokens =
        {
            new PartOfKeyword(),
            new Word(),
            new TerminalSymbol(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var hierarchy = ImportExportSyntax.Parse(ref parser);

        Assert.IsType<PartOfKeyword>(hierarchy?.Direction);

        Assert.Single(hierarchy.Components);
        Ronin.Grammar.Name name = hierarchy.Components[0];
        Assert.Equal(1, name?.Source.Length);     
    }

    [Fact(DisplayName = "with some hierarchy")]
    public void WithHierarchy()
    {
        // import standard funstuff websockets;

        Token[] tokens =
        {
            new ImportKeyword(),
            new Word(),
            new Word(),
            new Word(),
            new TerminalSymbol(),
            Sentinel.Instance
        };
                
        Parser parser = new(tokens);
        var hierarchy = ImportExportSyntax.Parse(ref parser);

        Assert.IsType<ImportKeyword>(hierarchy?.Direction);
        Assert.Single(hierarchy.Components);
        Ronin.Grammar.Name name = hierarchy.Components[0];
        Assert.Equal(3, name?.Source.Length);
    }

    [Fact(DisplayName = "keywords are just text")]
    public void WithKeywords()
    {
        // part of thing compiled to whatever secret stuff;

        Token[] tokens =
        {
            new PartOfKeyword(),
            new Word(),
            new CompiledKeyword(),
            new Word(),
            new Word(),
            new Word(),
            new Word(),
            new TerminalSymbol(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var hierarchy = ImportExportSyntax.Parse(ref parser);

        Assert.IsType<PartOfKeyword>(hierarchy?.Direction);
        Assert.Single(hierarchy.Components);
        Ronin.Grammar.Name name = hierarchy.Components[0];
        Assert.Equal(6, name?.Source.Length);
    }

    [Fact(DisplayName = "using text literal")]
    public void TextLiteral()
    {
        // part of literal testing "fast version" readonly;

        Token[] tokens =
        {
            new PartOfKeyword(),
            new Word(),
            new Word(),
            new TextLiteral(),
            new Word(),
            new TerminalSymbol(),
            Sentinel.Instance
        };
        
        Parser parser = new(tokens);
        var hierarchy = ImportExportSyntax.Parse(ref parser);

        Assert.IsType<PartOfKeyword>(hierarchy?.Direction);

        Assert.Equal(3, hierarchy.Components?.Count);

        {
            Ronin.Grammar.Name name = hierarchy.Components[0];
            Assert.Equal(2, name?.Source.Length);
        }

        {
            LiteralSyntax scalar = hierarchy.Components[1];
            Assert.Equal(1, scalar?.Source.Length);            
        }

        {
            Ronin.Grammar.Name name = hierarchy.Components[2];
            Assert.Equal(1, name?.Source.Length);
        }
    }
}