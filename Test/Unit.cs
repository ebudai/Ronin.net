using Ronin.Parser;

namespace Ronin.Transpiler.Test;

public class Unit
{
    [Fact]
    public void Parse()
    {
        var scope = Parser.Parser.Parse(new FileInfo("code.ronin"));
        Assert.NotNull(scope);
    }
}