using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;

using Function = Ronin.Grammar.Function;

namespace Unit;

/// <summary>
///     The iterating scope, from source.
/// </summary>
///
/// <remarks>
///     <para>
///     These called <c>Scope.Iterating.Parse</c> directly, on hand-built token
///     chains, which picks the intended production before the test starts — so
///     they could not have noticed that a datum was winning the same source in
///     <c>Statement.Parse</c>, and it was.
///     </para>
///     <para>
///     They also wrote «iterate cars =&gt; var car», and passed because
///     <c>Name.Parse</c> swallowed the «var» into a two-word name «var car» — a
///     spelling the grammar never had.
///     </para>
///     <para>
///     The spelling is «for each «a name» in «an expression» «a body»»,
///     decided in LOOPSYNTAX.md. It is safe because the loop variable is PINNED
///     to one word, so the split point is fixed by the pattern's shape — names
///     containing «in» are legal, and «in» is reserved nowhere; see LoopSyntax
///     for the case that proves it.
///     </para>
/// </remarks>
[Trait(nameof(Parser), null)]
public class IteratingScopes
{
    private static Statement Only(string source)
    {
        Lexer lexer = new(source);
        Parser parser = new(lexer.Lex());

        var module = parser.Parse();

        Assert.False(parser.IsNotFinished, $"«{source}» left input unconsumed");

        return Assert.Single(module.Scopes[0].Statements);
    }

    [Fact(DisplayName = "a loop over a name, with a block body")]
    public void ALoopOverANameWithABlockBody()
    {
        var loop = Assert.IsType<Scope.Iterating>(Only("for each car in cars { car speed = 9000; }\n"));

        Assert.NotNull(loop.Iterable);
        Assert.Equal("car", loop.Current.Words);

        Assert.IsType<Association>(Assert.Single(loop.Statements));
    }

    [Fact(DisplayName = "a loop is still a loop when its body is one statement")]
    public void ALoopIsStillALoopWhenItsBodyIsOneStatement()
    {
        var loop = Assert.IsType<Scope.Iterating>(Only("for each value in values { value = 1; }\n"));

        Assert.NotNull(loop.Iterable);
        Assert.Equal("value", loop.Current.Words);
    }

    [Fact(DisplayName = "a keyword-led scope is not a declaration named after its keyword")]
    public void AKeywordLedScopeIsNotADeclarationNamedAfterItsKeyword()
    {
        // Every one of these parsed as a Datum. A datum needs no mutability once
        // it has a type, Member.Parse is tried before Scope.Parse, and every
        // keyword is a Word — so «iterate banks => bank» was a declaration of
        // something called «iterate banks» with type «bank», and compiled clean.
        //
        // Whether the AST came out right depended on what followed the arrow:
        // «if ready => 1» stayed a conditional, because a number cannot be
        // mistaken for a type.
        Assert.IsType<Scope.Iterating>(Only("for each bank in banks { return bank; }\n"));
        Assert.IsType<Function>(Only("function f => Number { return 1; }\n"));
        Assert.IsType<Scope.Conditional<If>>(Only("if ready => result;\n"));
        Assert.IsType<Scope.Conditional<While>>(Only("while ready => result;\n"));
        Assert.IsType<Scope.Conditional<When>>(Only("when ready => result;\n"));
        Assert.IsType<Scope.Reactive>(Only("when changing ready => result;\n"));

        // the case that already worked, which is why the others went unnoticed
        Assert.IsType<Scope.Conditional<If>>(Only("if ready => 1;\n"));
    }

    [Fact(DisplayName = "a modifier may still begin a name")]
    public void AModifierMayStillBeginAName()
    {
        // The rule is about keywords that ANNOUNCE a production, and a modifier
        // in this position announces nothing — «var hidden cost» is a name the
        // language already accepts, so the fix must not take it away.
        var datum = Assert.IsType<Datum>(Only("var hidden cost => Number;\n"));

        Assert.Equal("hidden cost", datum.Identifier.Words);
    }
}
