// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Grammar = Ronin.Grammar;

namespace Unit;

/// <summary>
///     What the parser must refuse, against real source.
/// </summary>
///
/// <remarks>
///     Each of these produced an apparently valid tree from incomplete input,
///     and each went unseen for the same reason: parser tests asserted the
///     returned subtype and never that the whole input was consumed.
/// </remarks>
[Trait(nameof(Parser), null)]
public class Strictness
{
    private static Grammar.Module Parse(string source)
    {
        Lexer lexer = new(source);
        Parser parser = new(lexer.Lex());
        return parser.Parse();
    }

    [Fact(DisplayName = "while parses through the ordinary path")]
    public void WhileParsesThroughTheOrdinaryPath()
    {
        // Repeating existed and was never listed in Scope.Parse, so real source
        // routed a valid «while» into Unknown. Its unit tests passed because
        // they called Repeating.Parse themselves.
        Lexer lexer = new("while x { y = 1; }");
        Parser parser = new(lexer.Lex());

        Assert.IsType<Grammar.Scope.Conditional<Ronin.Lexicon.While>>(Grammar.Scope.Parse(ref parser));
    }

    [Fact(DisplayName = "an aggregate must be closed")]
    public void AnAggregateMustBeClosed()
    {
        // running out of tokens is not the same as being closed
        Lexer truncated = new("{ x = 1;");
        Parser parser = new(truncated.Lex());
        Assert.Null(Grammar.Scope.Basic.Parse(ref parser));

        Lexer closed = new("{ x = 1; }");
        Parser complete = new(closed.Lex());
        Assert.Single(Grammar.Scope.Basic.Parse(ref complete).Statements);
    }

    [Fact(DisplayName = "elements must be separated")]
    public void ElementsMustBeSeparated()
    {
        // A missing terminator between statements, which is where an omitted
        // separator is actually reachable. «(a b)» cannot show it: «a b» is one
        // two-word name, so that aggregate genuinely has one element.
        Lexer lexer = new("{ x = 1 y = 2; }");
        Parser parser = new(lexer.Lex());

        Assert.Null(Grammar.Scope.Basic.Parse(ref parser));
    }

    [Fact(DisplayName = "a trailing separator is allowed")]
    public void ATrailingSeparatorIsAllowed()
    {
        // the guide's own examples use one, and it makes for cleaner diffs
        Lexer lexer = new("(a, b,)");
        Parser parser = new(lexer.Lex());

        Assert.Equal(2, Grammar.Inputs.Parse(ref parser)?.Count);
    }

    [Fact(DisplayName = "adjacent words are one name, not two elements")]
    public void AdjacentWordsAreOneNameNotTwoElements()
    {
        // worth pinning, because it is why the obvious example of an omitted
        // separator does not demonstrate one
        Lexer lexer = new("(a b)");
        Parser parser = new(lexer.Lex());

        Assert.Single(Grammar.Inputs.Parse(ref parser));
    }

    [Fact(DisplayName = "trailing input is reported, not discarded")]
    public void TrailingInputIsReportedNotDiscarded()
    {
        // an unmatched delimiter meant everything after it silently did not exist
        var module = Assert.IsType<Grammar.Module.UnexpectedInputError>(Parse("var x => Number; ) var y => Number;"));

        Assert.Equal("unexpected input", module.Reason);
        Assert.NotEmpty(module.Tokens.ToArray());

        // and what parsed before it is kept
        Assert.Single(module.Scopes[0].Statements);
    }

    [Fact(DisplayName = "a whole file consumes to the sentinel")]
    public void AWholeFileConsumesToTheSentinel()
    {
        var module = Parse("var x => Number; var y => Number;");

        Assert.IsNotType<Grammar.Module.UnexpectedInputError>(module);
        Assert.Equal(2, module.Scopes[0].Statements.Count);
    }
}
