using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using System.Collections;
using Test;
using Literal = Ronin.Grammar.Literal;

namespace Unit;

[Trait(nameof(Parser), null)]
public class References : ParsingTests
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        // thing 7 ("stuff")

        List<Token> tokens = new()
        {
            Word("thing"),
            Number(7),
            StartValues(),
            Text("stuff"),
            EndValues(),
            new Sentinel()
        };
        
        Parser parser = new(tokens.AsLinkedList());
        var reference = Reference.Parse(ref parser);

        Assert.Equal(3, reference?.Span.Length);

        {
            var name = reference.Span[0].AsName;
            Assert.Single(name?.Tokens.ToArray());
        }

        {
            var scalar = reference.Span[1].AsTemporary as Literal;
            Assert.Single(scalar?.Tokens.ToArray());
        }

        {
            var arguments = reference.Span[2].AsTemporary as Inputs;
            Assert.Single(arguments);
            var scalar = arguments[0].AsValue as Literal;
            Assert.Single(scalar?.Tokens.ToArray());
        }
    }

    [Fact(DisplayName = "enumerable")]
    public void Enumerable()
    {
        List<Token> tokens = new()
        {
            Word("thing"),
            Number(7),
            StartValues(),
            Text("stuff"),
            EndValues(),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var reference = Reference.Parse(ref parser);
        IEnumerable enumerable = reference;

        Assert.Equivalent(enumerable.GetEnumerator(), reference.GetEnumerator());
    }
}
