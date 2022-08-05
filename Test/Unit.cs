namespace Ronin.Test;

public class Unit
{
    [Fact]
    public void Parse()
    {
        Parser.Parser parser = new(new FileInfo("code.ronin"));

        var scope = parser.ParseScope();

        Assert.NotNull(scope);
        Assert.Equal(2, scope.Expressions.Count);

    }
}