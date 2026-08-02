// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar;

namespace Integration;

/// <summary>
///     Where the heading of a scope ends, which is at the brace that opens its
///     body.
/// </summary>
///
/// <remarks>
///     <para>
///     An anonymous value after a word is an argument — «thing 7 ("stuff")» is
///     one call — and a brace opens one. So a heading read «if c { 1 }» as the
///     reference «c» applied to the list «{ 1 }», found no body after it, and
///     the whole statement was malformed. Every conditional and every loop whose
///     body was a single bare expression went the same way.
///     </para>
///     <para>
///     It was invisible because a «;» is what stopped «{ 1 }» being a list:
///     «if c { 1; }» compiled and «if c { 1 }» did not, and every body in the
///     suite happened to end in one. The same habit as the one
///     <see cref="StatementShapes"/> was written for, one construct along.
///     </para>
/// </remarks>
[Trait(nameof(Parser), null)]
public class ScopeHeadings
{
    private static Compilation Of(string source) => Compilation.Of(new SourceText(source, "Heading.ron"));

    private static Statement Only(string source)
    {
        var compilation = Of(source);

        Assert.Empty(compilation.Findings);

        return Assert.Single(compilation.Module.Scopes[0].Statements);
    }

    [Theory(DisplayName = "a body that is one bare expression needs no «;» to be found")]
    [InlineData("if c { 1 }\n")]
    [InlineData("while c { 1 }\n")]
    [InlineData("when c { 1 }\n")]
    [InlineData("when changing c { 1 }\n")]
    [InlineData("for each i in c { 1 }\n")]
    public void ABodyThatIsOneBareExpressionNeedsNoTerminatorToBeFound(string source)
    {
        // Every one of these was malformed. The «;» in «if c { 1; }» was doing
        // work nobody had asked it to do, and the shape most likely to be
        // written first — a one-line body — was the shape that did not compile.
        Assert.IsAssignableFrom<Scope>(Only(source));
    }

    [Theory(DisplayName = "and the heading is still whatever it was")]
    [InlineData("if a b { 1 }\n")]
    [InlineData("if a + b { 1 }\n")]
    [InlineData("if c (x) { 1 }\n")]
    [InlineData("if takes ({ 1 }) { 2 }\n")]
    public void AndTheHeadingIsStillWhateverItWas(string source)
    {
        // A multi-word condition, an operator in one, and a call in one. The
        // last two are the case a bracket has to survive: an aggregate parses
        // its elements outside the heading, and the parser it wrote back said
        // so — the caller's heading ended at its first bracket, and the body
        // went back to being an argument one call later.
        Assert.IsAssignableFrom<Scope>(Only(source));
    }

    [Theory(DisplayName = "and a brace that is not a body is still a value")]
    [InlineData("var x = { 1 };\n")]
    [InlineData("var y = { { 1 }, { 2 } };\n")]
    [InlineData("if c { { 1 } }\n")]
    [InlineData("if c { a = { 1 }; }\n")]
    [InlineData("if c { if d { 1 } }\n")]
    public void AndABraceThatIsNotABodyIsStillAValue(string source)
    {
        // The rule is the heading's and reaches no further. Inside a body, and
        // in ordinary value position, a brace means what it always meant — and
        // a nested «if» has a heading of its own that ends at its own brace.
        Assert.NotNull(Only(source));
    }

    [Fact(DisplayName = "a braced value in a heading costs brackets")]
    public void ABracedValueInAHeadingCostsBrackets()
    {
        // The whole bill, and it is not a finding. A list literal in heading
        // position is the one thing this takes away: the first brace is the
        // body, so «if takes { 1 } { 2 }» is a conditional whose body is «{ 1 }»
        // followed by a loose «{ 2 }» — which is two statements, both legal.
        //
        // Worth pinning because it is a silent reading and not an error, and it
        // was malformed before. Whether a loose braced value should be a finding
        // in its own right is a question for the designer and not this rule's.
        var compilation = Of("if takes { 1 } { 2 }\n");

        Assert.Empty(compilation.Findings);
        Assert.Collection(compilation.Module.Scopes[0].Statements,
                          first => Assert.IsAssignableFrom<Scope>(first),
                          second => Assert.IsType<List>(second));

        // and bracketing gives the argument back
        Assert.IsAssignableFrom<Scope>(Only("if takes ({ 1 }) { 2 }\n"));
    }
}
