using Ronin.Parser;

namespace Unit;

public class NumberLiteral : LiteralUnitTest
{
    public NumberLiteral() : base("literals\\number") { }

    [Fact(DisplayName = "parse number literal")]
    public void Literal() => Test(0, "decimal literal =", "107.2", Scalar.number);
}
