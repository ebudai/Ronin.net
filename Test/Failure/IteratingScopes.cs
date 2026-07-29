using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;

namespace Failure;

/// <summary>
///     A loop header that does not hold together, from source.
/// </summary>
///
/// <remarks>
///     These built token chains and called <c>Scope.Parse</c>, which chooses the
///     production before the test begins. From source instead, so each case also
///     says that no OTHER production wins it — which is how «iterate banks =&gt;
///     bank» came to parse as a declaration for a year without anything noticing.
/// </remarks>
[Trait(nameof(Parser), null)]
public class IteratingScopes
{
    private static Statement Only(string source)
    {
        Lexer lexer = new(source);
        Parser parser = new(lexer.Lex());

        return Assert.Single(parser.Parse().Scopes[0].Statements);
    }

    [Fact(DisplayName = $"doesn't start with {ForEach.keyword}")]
    public void NotALoop()
    {
        Lexer lexer = new("not loop;\n");
        Parser parser = new(lexer.Lex());

        Assert.Null(Scope.Parse(ref parser));
    }

    [Fact(DisplayName = "no variable to bind")]
    public void NoVariableToBind()
    {
        // «7» is not a name at all, and the loop variable is a declaration site
        Assert.IsType<Scope.Iterating.ExpectedNameError>(Only("for each 7 in horses { run the horse; }\n"));

        // «in» is an ordinary word now, so «for each in horses» takes «in» as
        // the variable and then finds «horses» where the separator should be
        Assert.IsType<Scope.Iterating.ExpectedInError>(Only("for each in horses { run the horse; }\n"));
    }

    [Fact(DisplayName = "missing 'in'")]
    public void MissingIn()
    {
        // «for each car cars fast colour = 3;» — no «in», so nothing separates
        // the variable from what it walks
        Assert.IsType<Scope.Iterating.ExpectedInError>(Only("for each car cars fast colour = 3;\n"));
    }

    [Fact(DisplayName = "nothing at all where the separator goes")]
    public void NothingAtAllWhereTheSeparatorGoes()
    {
        // Not merely the wrong word — no word. The variable is pinned to one
        // token, so whatever follows it has to be «in» and there may be nothing
        // else there at all.
        Assert.IsType<Scope.Iterating.ExpectedInError>(Only("for each bank;\n"));
    }

    [Fact(DisplayName = "a bracketed variable that is not a name")]
    public void ABracketedVariableThatIsNotAName()
    {
        // Brackets are how a multi-word loop variable is written now, so the two
        // ways of getting them wrong need saying: nothing inside them, and
        // nothing closing them.
        Assert.IsType<Scope.Iterating.ExpectedNameError>(Only("for each () in horses { run the horse; }\n"));
        Assert.IsType<Scope.Iterating.ExpectedNameError>(Only("for each (fast horse in horses { run it; }\n"));
    }

    [Fact(DisplayName = "missing iterable")]
    public void MissingIterable()
    {
        Assert.IsType<Scope.Iterating.ExpectedIterableError>(Only("for each car in = 3;\n"));
    }
}
