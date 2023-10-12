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
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var import = Import.Parse(ref parser);

        var module = import?.Module as Module.Unresolved;
        Assert.Single(module?.Name.Tokens.ToArray());
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
            new Sentinel()
        };
                
        Parser parser = new(tokens.AsLinkedList());
        var import = Import.Parse(ref parser);

        var module = import?.Module as Module.Unresolved;
        Assert.Equal(3, module?.Name?.Tokens.Length);
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
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var import = Import.Parse(ref parser);

        var module = import?.Module as Module.Unresolved;
        Assert.Equal(6, module?.Name.Tokens.Length);
    }

    /*[Trait(nameof(Analyzer), nameof(Declaration))]
    public class Declaration
    {
        [Fact(DisplayName = $"basic")]
        public void Basic()
        {
            const string thing = nameof(thing);
            const string with = nameof(with);
            const string stuff = nameof(stuff);

            *//*
             
             {
                import thing with stuff;
             }
             
             *//*

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
            analyzer.DefineScope(analyzer.Global, scope);
            Assert.Empty(analyzer.Errors);

            Assert.Single(scope.Definition.Imports);
            Assert.Empty(analyzer.Global.Imports);

            var import = scope.Definition.Imports.First();
            Assert.IsType<Module.Unresolved>(import);
        }
    }*/
}