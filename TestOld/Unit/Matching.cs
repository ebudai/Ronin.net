namespace Unit;

public class Matching : UnitTest
{
    public Matching() : base("matching") { }

    [Fact(DisplayName = "simple match")]
    public void Simple()
    {
        Assert.NotEmpty(scope.Expressions);

        var syntax = scope.Expressions[0].Syntax;
    }
}
