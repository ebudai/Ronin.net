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

    [Theory(DisplayName = "a declaration's heading ends there too")]
    [InlineData("function f => Number { 1 }\n")]
    [InlineData("function f => Number {}\n")]
    [InlineData("function f => Number { 1; }\n")]
    [InlineData("function f => Number { return 1; }\n")]
    [InlineData("type T = Base {}\n")]
    [InlineData("type T = Base { var a => Number; }\n")]
    public void ADeclarationsHeadingEndsThereToo(string source)
    {
        // Found by audit, and the finding is really about the LIST: the rule was
        // applied to a hand-maintained set of scope classes, and these two are
        // the same join with a different base class. A function's return type
        // and a type's algebra are both a reference that a definition follows.
        //
        // «type T = Base {}» is the one that says nothing. A type may legally
        // have no members, so the algebra took the brace and there was no
        // later failure to reveal it — the members were null where an empty
        // definition belonged.
        Assert.NotNull(Only(source));
    }

    [Fact(DisplayName = "and the empty body of a derived type is a body, not the absence of one")]
    public void AndTheEmptyBodyOfADerivedTypeIsABodyNotTheAbsenceOfOne()
    {
        // The assertion the silent case needs. Parsing without a finding was
        // never the question: «type T = Base {}» did that before, with its
        // brace inside the algebra.
        var derived = Assert.IsType<Ronin.Grammar.Type>(Only("type T = Base {}\n"));

        Assert.NotNull(derived.Members);
        Assert.Empty(derived.Members);

        // and one that declares no algebra reaches the same place
        Assert.Empty(Assert.IsType<Ronin.Grammar.Type>(Only("type T {}\n")).Members);
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
    [InlineData("if takes ([ 1 ]) { 2 }\n")]
    public void AndTheHeadingIsStillWhateverItWas(string source)
    {
        // A multi-word condition, an operator in one, and a call in one. The
        // last two are the case a bracket has to survive: an aggregate parses
        // its elements outside the heading, and the parser it wrote back said
        // so — the caller's heading ended at its first bracket, and the body
        // went back to being an argument one call later.
        Assert.IsAssignableFrom<Scope>(Only(source));
    }

    [Theory(DisplayName = "and a value beside a heading is still a value")]
    [InlineData("var x = [ 1 ];\n")]
    [InlineData("var y = [ [ 1 ], [ 2 ] ];\n")]
    [InlineData("if c { [ 1 ] }\n")]
    [InlineData("if c { a = [ 1 ]; }\n")]
    [InlineData("if c { if d { 1 } }\n")]
    public void AndAValueBesideAHeadingIsStillAValue(string source)
    {
        // The rule is the heading's and reaches no further. A nested «if» has a
        // heading of its own that ends at its own brace, and a list is a list
        // wherever it appears.
        Assert.NotNull(Only(source));
    }

    [Fact(DisplayName = "and a brace in a heading can only be the body now")]
    public void AndABraceInAHeadingCanOnlyBeTheBodyNow()
    {
        // This used to be the bill. A list was braced, so «if takes { 1 } { 2 }»
        // could read the first brace as an argument, and the heading rule was
        // what stopped it — at the cost of a braced literal in heading position,
        // which brackets bought back.
        //
        // Lists and lookups are bracketed now, so nothing a heading could
        // absorb begins with a brace and there is no bill left to pay. The
        // second brace is a block of its own, which is two statements and legal.
        var compilation = Of("if takes { 1 } { 2 }\n");

        Assert.Empty(compilation.Findings);
        Assert.Equal(2, compilation.Module.Scopes[0].Statements.Count);

        // and the argument no longer needs buying back
        Assert.IsAssignableFrom<Scope>(Only("if takes [ 1 ] { 2 }\n"));
    }
}
