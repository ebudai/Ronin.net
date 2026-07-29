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

        // and «in» first means the split leaves nothing on the left of it —
        // a header that is all collection and no variable
        Assert.IsType<Scope.Iterating.ExpectedNameError>(Only("for each in horses { run the horse; }\n"));
    }

    [Fact(DisplayName = "missing 'in'")]
    public void MissingIn()
    {
        // «for each car cars fast colour = 3;» — no «in», so nothing separates
        // the variable from what it walks
        Assert.IsType<Scope.Iterating.ExpectedInError>(Only("for each car cars fast colour = 3;\n"));
    }

    [Fact(DisplayName = "missing iterable")]
    public void MissingIterable()
    {
        Assert.IsType<Scope.Iterating.ExpectedIterableError>(Only("for each car in = 3;\n"));
    }
}
