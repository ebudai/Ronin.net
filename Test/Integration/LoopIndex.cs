// Copyright © 2026 Eric Budai

using Ronin.Compiler;

namespace Integration;

/// <summary>
///     A loop injects a counter named after its variable.
/// </summary>
///
/// <remarks>
///     <para>
///     «index of bank», not a bare «index». There is no shadowing in this
///     language, so a bare one would collide with every «var index» anyone
///     writes — and "rename your variable because the loop wanted the word" is
///     the diagnostic the whole grammar exists to avoid.
///     </para>
///     <para>
///     Derived, three things fall out with no rules attached: nested loops do
///     not collide, the author controls the name by controlling the loop
///     variable, and it reads as prose. It is subject to the scope rules like
///     any injected name, and its diagnostics name the loop variable — because
///     «index of bank» is not the programmer's to rename.
///     </para>
/// </remarks>
[Trait(nameof(Compilation), null)]
public class LoopIndex
{
    private static Compilation Of(string source) => Compilation.Of(new SourceText(source, "Player.ron"));

    private static IReadOnlySet<string> Names(string source)
    {
        var compilation = Of(source);

        Assert.Empty(compilation.Findings);

        return compilation.Declarations.Symbols.Names;
    }

    [Fact(DisplayName = "the counter is in scope inside the loop")]
    public void TheCounterIsInScopeInsideTheLoop()
    {
        // 1 and 6. Inside only: the body is its own scope, so nothing leaks out
        // to a sibling or to the file. Whether it ADVANCES is a runtime question
        // and waits on the evaluator being joined.
        var inside = Names("""
                           for each bank in banks { return index of bank; }
                           var elsewhere => Number;

                           """);

        Assert.DoesNotContain("index of bank", inside);
        Assert.DoesNotContain("bank", inside);
        Assert.Contains("elsewhere", inside);
    }

    [Fact(DisplayName = "nested loops each get their own")]
    public void NestedLoopsEachGetTheirOwn()
    {
        // 2. Derived from the variable, so they cannot collide — a bare «index»
        // would need shadowing rules to nest, and this language has none.
        Assert.Empty(Of("""
                        for each bank in banks
                        {
                            for each branch in branches { return index of branch; }

                            return index of bank;
                        }

                        """).Findings);
    }

    [Fact(DisplayName = "a user's own name for it collides, and both sites are named")]
    public void AUsersOwnNameForItCollidesAndBothSitesAreNamed()
    {
        // 3. The injected name is a declaration like any other, so declaring it
        // yourself is shadowing — and the message can point at the loop, which
        // is the thing to change.
        var finding = Assert.Single(Of("""
                                       for each bank in banks { var index of bank => Number; }

                                       """).Findings);

        var shadowed = Assert.IsType<Shadowed>(finding);

        Assert.Equal("index of bank", shadowed.Name);
        Assert.Equal("in this scope", shadowed.Where);

        // the loop variable is where the injection came from
        Assert.Equal("first declared here", Assert.Single(shadowed.Related).Label);
    }

    [Fact(DisplayName = "the counter follows the variable's name")]
    public void TheCounterFollowsTheVariablesName()
    {
        // 5. Renaming the loop variable renames it, which is what makes the
        // collision above a local, obvious edit.
        Assert.Contains("index of holding", Body("for each holding in banks { return index of holding; }\n"));
        Assert.DoesNotContain("index of bank", Body("for each holding in banks { return index of holding; }\n"));
    }

    [Fact(DisplayName = "a counter that breaks a rule blames the loop, not itself")]
    public void ACounterThatBreaksARuleBlamesTheLoopNotItself()
    {
        // 4, and the reason the injected-name finding exists at all. «of» is
        // glue in «item (_) of (_)», so the counter contains glue while the loop
        // variable does not — a shape «old x» could never produce, because it
        // adds only the one word the segment rules already refuse.
        var findings = Of("""
                          function item (which => Number) of (list => Number) { return which; }
                          for each bank in banks { return bank; }

                          """).Findings;

        var glue = Assert.IsType<GlueInInjectedName>(Assert.Single(findings, finding => finding is GlueInInjectedName));

        Assert.Equal("index of bank", glue.Name);
        Assert.Equal("bank", glue.Injector);
        Assert.Equal("of", glue.Word);
        Assert.Equal("item (_) of (_)", glue.Pattern);
    }

    /// <summary>The names the loop body declares, which is a nested scope.</summary>
    private static IReadOnlyCollection<string> Body(string source)
    {
        var compilation = Of(source);

        Assert.Empty(compilation.Findings);

        // the body's scope is not the module's, so this reaches it the way the
        // walk does rather than by asking the outer table
        var loop = compilation.Module.Scopes[0].Statements.OfType<Ronin.Grammar.Scope.Iterating>().Single();

        return Ronin.Grammar.Declarations
                    .Of(loop.Statements, compilation.Source, compilation.Declarations, loop.Current)
                    .Symbols.Names;
    }
}
