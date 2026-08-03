// Copyright © 2026 Eric Budai

using Ronin.Compiler;

namespace Integration;

/// <summary>
///     A «when» belongs at module scope, and nowhere a scope can close around
///     it.
/// </summary>
///
/// <remarks>
///     <para>
///     A propagation step happens BETWEEN statements rather than during one, so
///     a «when» declared inside a function body has two possible lifetimes and
///     both are wrong: it leaves its scope before any step runs, in which case
///     it can never fire and the declaration is dead; or it outlives its scope,
///     in which case it holds references to locals that are gone.
///     </para>
///     <para>
///     There is no third option, so the restriction costs nothing — and it is
///     what lets the lifetime rule be stated whole, which is why it is worth
///     having before there is a runtime that would have to honour it.
///     </para>
/// </remarks>
[Trait(nameof(Compilation), null)]
public class WhenPlacement
{
    private static IReadOnlyList<Finding> Of(string source)
        => Compilation.Of(new SourceText(source, "Player.ron")).Findings;

    [Theory(DisplayName = "a «when» at module scope is where it belongs")]
    [InlineData("when ready { return 1; }")]
    [InlineData("when changing ready { return 1; }")]
    [InlineData("var ready => Number;\nwhen ready { return 1; }")]
    public void AWhenAtModuleScopeIsWhereItBelongs(string source) => Assert.Empty(Of(source + "\n"));

    [Theory(DisplayName = "and anywhere a scope closes around it, it is refused")]
    // both spellings, because «when changing x» and «when x» are different
    // productions and only one of them is the type the other's name suggests
    [InlineData("function update path { when ready { return 1; } }", "«update path»")]
    [InlineData("function update path { when changing ready { return 1; } }", "«update path»")]
    [InlineData("for each bank in banks { when ready { return 1; } }", "a loop")]
    [InlineData("when ready { when other { return 1; } }", "another «when»")]
    [InlineData("var c = (x) => { when ready { return 1; } };", "a delegate")]
    [InlineData("compiled { when ready { return 1; } }", "a block")]
    [InlineData("if ready { when other { return 1; } }", "a block")]
    [InlineData("while ready { when other { return 1; } }", "a block")]
    public void AndAnywhereAScopeClosesAroundItItIsRefused(string source, string inside)
    {
        var misplaced = Assert.IsType<MisplacedWhen>(Assert.Single(Of(source + "\n")));

        // named the way a reader would, because "inside a scope" is true of the
        // module too and says nothing about what to change
        Assert.Equal(inside, misplaced.Inside);
    }

    [Theory(DisplayName = "inside a type it is refused by name, not as a syntax error")]
    [InlineData("type Box { when ready { return 1; } }")]
    [InlineData("type Box { when changing ready { return 1; } }")]
    [InlineData("type Box { var a => Number; when ready { return 1; } }")]
    public void InsideATypeItIsRefusedByNameNotAsASyntaxError(string source)
    {
        // A type «when» is designed — it lives as long as the instance — and the
        // instances are built now; what is missing is the join that fires one
        // type-scope node per instance. A user who writes one has understood the
        // design, and was being told they made a syntax error: a type's members
        // are an aggregate of Member, a «when» is a Scope, so the body simply
        // failed to parse and came back as «unexpected input», the same message
        // as a stray bracket.
        //
        // Recognising it to refuse it adds a message and no semantics.
        Assert.IsType<WhenInType>(Assert.Single(Of(source + "\n")));
    }

    [Theory(DisplayName = "a «when» nobody could parse is malformed, not refused by name")]
    [InlineData("type Box { when { return 1; } }")]
    [InlineData("type Box { when changing { return 1; } }")]
    public void AWhenNobodyCouldParseIsMalformedNotRefusedByName(string source)
    {
        // Found by audit. A parse-error node for a «when» is a reactive Scope
        // too — the error types inherit from the real ones — so the
        // first-invalid-element rule accepted one, and it has no keyword to
        // point at. The null token went into the finding and took the compiler
        // out on it.
        //
        // Recognising a construct in order to refuse it well requires having
        // recognised one.
        Assert.Equal(FindingKind.Malformed, Assert.Single(Of(source + "\n")).Kind);
    }

    [Fact(DisplayName = "and a genuine syntax error in a type still says so")]
    public void AndAGenuineSyntaxErrorInATypeStillSaysSo()
    {
        // The other half, and the reason the first matters: the loose re-read
        // exists to tell these apart, so it must not swallow everything. A type
        // body that holds no «when» is refused exactly as it was.
        Assert.Equal(FindingKind.Malformed, Assert.Single(Of("type Box { + }\n")).Kind);

        Assert.Empty(Of("type Box { var a => Number; }\n"));
    }

    [Fact(DisplayName = "the caret is on the «when», not its body or its condition")]
    public void TheCaretIsOnTheWhenNotItsBodyOrItsCondition()
    {
        // The body is not the mistake and neither is the condition. Both «when»
        // forms read the keyword to build their trigger and then let it go, so
        // the token is kept rather than counted back to.
        var misplaced = Assert.IsType<MisplacedWhen>(
            Assert.Single(Of("function f { when ready { return 1; } }\n")));

        // 1:14 is the «when», not the «{» at 26 nor the condition at 19
        Assert.Equal("Player.ron:1:14", Diagnostics.Report(misplaced).Split(':')[0..3] is var parts
                                      ? string.Join(':', parts)
                                      : null);

        Assert.Equal("when", misplaced.Primary.Source.Text.Substring(misplaced.Primary.Offset,
                                                                    misplaced.Primary.Length));
    }
}
