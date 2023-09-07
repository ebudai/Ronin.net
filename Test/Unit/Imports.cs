using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;
using Import = Ronin.Grammar.Import;

namespace Unit;

[Trait(nameof(Parser), null)]
public class Imports : ParsingTests  
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // import things;

        List<Token> tokens = new()
        {
            Keyword.Import(),
            Word("things"),
            Terminal(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var import = Import.Parse(ref parser);

        Assert.Equal(1, import.Name?.Source.Length);     
    }

    [Fact(DisplayName = "with some hierarchy")]
    public void WithExport()
    {
        // import standard funstuff websockets;

        List<Token> tokens = new()
        {
            Keyword.Import(),
            Word("standard"),
            Word("funstuff"),
            Word("websockets"),
            Terminal(),
            Sentinel.Instance
        };
                
        Parser parser = new(tokens);
        var import = Import.Parse(ref parser);

        Assert.Equal(3, import.Name?.Source.Length);
    }

    [Fact(DisplayName = "keywords are just text")]
    public void WithKeywords()
    {
        // import thing compiled to whatever secret stuff;

        List<Token> tokens = new()
        {
            Keyword.Import(),
            Word("thing"),
            Keyword.Compiled(),
            Word("to"),
            Word("whatever"),
            Word("secret"),
            Word("stuff"),
            Terminal(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var import = Import.Parse(ref parser);

        Assert.Equal(6, import.Name?.Source.Length);
    }

    [Trait(nameof(Analyzer), nameof(Declaration))]
    public class Declaration
    {
        [Fact(DisplayName = $"basic")]
        public void Basic()
        {
            const string thing = nameof(thing);
            const string with = nameof(with);
            const string stuff = nameof(stuff);

            /*
             
             {
                import thing with stuff;
             }
             
             */

            AnonymousScope scope = new()
            {
                Definition = new()
                {
                    new Import
                    {
                        Name = new() { Source = new[] { Word(thing), Word(with), Word(stuff) } }
                    }
                }
            };

            Analyzer analyzer = new();
            analyzer.Define(analyzer.Global, scope);
            Assert.Empty(analyzer.Errors);

            Assert.Single(scope.Definition.GetImports());
            Assert.Empty(analyzer.Global.GetImports());

            var import = scope.Definition.GetImports().First();
            Assert.IsType<Context.Unresolved>(import);
        }
    }
}