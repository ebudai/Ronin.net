using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Test;

namespace Unit;

[Trait("Parser", null)]
public class Exports : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // part of things;

        List<Token> tokens = new()
        {
            Keyword.PartOf(),
            Word("things"),
            Terminal(),
            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var export = Export.Parse(ref parser);

        Assert.Equal(1, export.Name?.Source.Length);     
    }

    [Fact(DisplayName = "with some hierarchy")]
    public void WithExport()
    {
        // part of standard funstuff websockets;

        List<Token> tokens = new()
        {
            Keyword.PartOf(),
            Word("standard"),
            Word("funstuff"),
            Word("websockets"),
            Terminal(),
            Sentinel.Instance
        };
                
        Parser parser = new(tokens);
        var export = Export.Parse(ref parser);

        Assert.Equal(3, export.Name?.Source.Length);
    }

    [Fact(DisplayName = "keywords are just text")]
    public void WithKeywords()
    {
        // part of thing compiled to whatever secret stuff;

        List<Token> tokens = new()
        {
            Keyword.PartOf(),
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
        var export = Export.Parse(ref parser);

        Assert.Equal(6, export.Name?.Source.Length);
    }

    [Trait("Analyzer", "declaration")]
    public class Declaration
    {
        [Fact(DisplayName = "basic")]
        public void Basic()
        {
            const string thing = nameof(thing);
            const string with = nameof(with);
            const string stuff = nameof(stuff);

            // { part of thing with stuff; }

            AnonymousScope module = new()
            {
                Definition = new()
                {
                    Values = new List<Statement>
                    {
                        new Export
                        {
                            Name = new() { Source = new[] { Word(thing), Word(with), Word(stuff) } }
                        }
                    }
                }            
            };

            List<Error> errors = new();
            Analyzer.Define(Global.Scope, module, errors);
            Assert.Empty(errors);

            Assert.Single(Global.Scope.Children);

            Name name = Global.Scope.Children.First().Key;

            Assert.Equal(3, name.Source.Length);
            Assert.Equal(thing, name.Source.Span[0].Memory.ToArray());
            Assert.Equal(with, name.Source.Span[1].Memory.ToArray());
            Assert.Equal(stuff, name.Source.Span[2].Memory.ToArray());
        }
    }
}