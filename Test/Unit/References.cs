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

    [Fact(DisplayName = "symbols are components, not part of a name")]
    public void SymbolsAreComponents()
    {
        // x > 3
        List<Token> tokens = new()
        {
            Word("x"),
            Symbol(">"),
            Number(3),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var reference = Reference.Parse(ref parser);

        // three components, not one name spanning «x >» — the parser records that a
        // symbol occurred and leaves what it means to the resolver
        Assert.Equal(3, reference?.Span.Length);
        Assert.NotNull(reference[0].AsName);
        Assert.NotNull(reference[1].AsSymbolic);
        Assert.NotNull(reference[2].AsTemporary);
        Assert.Equal(">", reference[1].AsSymbolic.Token.Memory.ToString());
    }

    [Fact(DisplayName = "punctuation ends a reference")]
    public void PunctuationEndsAReference()
    {
        // x; y   — the terminator is a boundary, not a component
        List<Token> tokens = new()
        {
            Word("x"),
            Terminal(),
            Word("y"),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());
        var reference = Reference.Parse(ref parser);

        Assert.Single(reference);
        Assert.NotNull(reference[0].AsName);
    }

    [Fact(DisplayName = "only a symbol parses as symbolic")]
    public void OnlyASymbolParsesAsSymbolic()
    {
        // Reference.Component only offers Symbolic a token that is not a name and
        // not a value, but the guard is the component's own, not the caller's.
        List<Token> tokens = new() { Word("x"), Terminal(), Symbol("+"), new Sentinel() };

        Parser parser = new(tokens.AsLinkedList());

        Assert.Null(Symbolic.Parse(ref parser));   // a word is not symbolic
        parser.Advance();
        Assert.Null(Symbolic.Parse(ref parser));   // nor is punctuation
        parser.Advance();
        Assert.NotNull(Symbolic.Parse(ref parser));
    }

    [Fact(DisplayName = "symbols alone are not a reference")]
    public void SymbolsAloneAreNotAReference()
    {
        // + *  — punctuating nothing
        List<Token> tokens = new()
        {
            Symbol("+"),
            Symbol("*"),
            new Sentinel()
        };

        Parser parser = new(tokens.AsLinkedList());

        Assert.Null(Reference.Parse(ref parser));
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
