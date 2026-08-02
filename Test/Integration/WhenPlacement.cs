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
