using Ronin.Compiler;
using Ronin.Grammar;
using Test;

namespace Failure;

[Trait("General", null)]
public class DeveloperMistake : ParsingTests
{
    class UnhandledSubclass : Statement { }

    [Fact(DisplayName = "unhandled subclass")]
    public void Unhandled()
    {
        const string thing = nameof(thing);
        const string with = nameof(with);
        const string stuff = nameof(stuff);

        Ronin.Lexicon.PartOf keyword = new();
        var tokens = new[] { Word(thing), Word(with), Word(stuff) };
        Name name = new() { Source = tokens };        
        Statement statement = new Export { Name = name, Source = new[] { keyword, tokens[0], tokens[1], tokens[2] } };

        var error = statement switch
        {
            Import => new Error("success"),
            _ => Error.UnhandledSubclass<Statement>(statement.GetType())
        };

        Assert.Equal("developer mistake", error.Reason);
        
        Assert.Equal(2, error.Data.Count);

        Assert.True(error.Data.TryGetValue("parent", out var parent));
        Assert.IsAssignableFrom<Type>(parent);
        Assert.Equal(typeof(Statement), parent);

        Assert.True(error.Data.TryGetValue("type", out var type));
        Assert.IsAssignableFrom<Type>(type);
        Assert.Equal(typeof(Export), type);
    }
}
