using Ronin.Parser;

namespace Unit;

public class MoneyLiteral : LiteralUnitTest
{
    public MoneyLiteral() : base("literals\\money") { }

    [Fact(DisplayName = "parse money literal")]
    public void Literal() => Test(0, "cash money =", "$17.20", Scalar.money);

    [Fact(DisplayName = "parse money literal from whole number")]
    public void FromWhole() => Test(1, "new money =", "$100000000", Scalar.money);

    [Fact(DisplayName = "parse large money literal")]
    public void LargeLiteral() => Test(2, "old money =", "$89643516846857432.98435403435135", Scalar.money);
}
