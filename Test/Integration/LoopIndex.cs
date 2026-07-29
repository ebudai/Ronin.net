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

    [Fact(DisplayName = "declaring it first collides too, and says so from the other side")]
    public void DeclaringItFirstCollidesTooAndSaysSoFromTheOtherSide()
    {
        // The order that used to kill the compiler. The injected counter went
        // into the symbol set without going through the refusal, so the second
        // «index of bank» was absorbed silently while the diagnostic metadata
        // took a second entry — and that metadata is keyed by name, so the
        // process died on the collision it existed to report. Declaring it
        // INSIDE the loop was refused correctly the whole time, which is why one
        // order passing proved nothing about the other.
        var shadowed = Assert.IsType<Shadowed>(Assert.Single(Of("""
                                                                var index of bank => Number;
                                                                for each bank in banks { return bank; }

                                                                """).Findings));

        Assert.Equal("index of bank", shadowed.Name);

        // The loop is the later declaration, so it is the one asked to give way
        // — and «in an enclosing scope», because the var is at file level and
        // the counter is injected into the body.
        Assert.Equal("in an enclosing scope", shadowed.Where);
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

    [Fact(DisplayName = "a pattern that would break the counter is refused, once")]
    public void APatternThatWouldBreakTheCounterIsRefusedOnce()
    {
        // 4, and the shape of the answer changed while this was being written.
        // «of» is how the counter's name is built, so a pattern making it glue
        // would make every loop in scope illegal — and the rule catches that at
        // the PATTERN, which is one finding with one repair.
        //
        // It used to also run the invalid pattern through the name scan, so each
        // loop collected its own complaint about a mistake it did not make. Now
        // a structurally invalid pattern takes no further part, which is what
        // makes the count independent of how many loops the file has.
        foreach (var loops in (int[])[0, 1, 3])
        {
            var source = "function item (which => Number) of (list => Number) { return which; }\n"
                       + string.Concat(Enumerable.Range(0, loops)
                                                 .Select(each => $"for each bank{each} in banks {{ return bank{each}; }}\n"));

            var finding = Assert.Single(Of(source).Findings);

            Assert.Equal("item (_) of (_)", Assert.IsType<InjectionWordAsGlue>(finding).Pattern);
        }
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
