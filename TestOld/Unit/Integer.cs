using Ronin.Parser;

namespace Unit;

public class IntegerLiteral : LiteralUnitTest
{
    public IntegerLiteral() : base("literals\\integer") { }

    [Fact(DisplayName = "parse integer literal")]
    public void Literal() => Test(0, "normal int =", "92804", Scalar.integer);

    [Fact(DisplayName = "parse tiny integer literal")]
    public void TinyLiteral() => Test(1, "tiny integer =", "5i8", Scalar.int8);

    [Fact(DisplayName = "parse small integer literal")]
    public void SmallLiteral() => Test(2, "smallint =", "1000  i16", Scalar.int16);

    [Fact(DisplayName = "parse large integer literal via suffix")]
    public void LargeSuffixLiteral() => Test(3, "large integer =", "65462168135136i64", Scalar.int64);

    [Fact(DisplayName = "parse large integer literal via value")]
    public void LargeValueLiteral() => Test(4, "another large integer =", "69843516843518656", Scalar.int64);

    [Fact(DisplayName = "parse arbitrary integer literal")]
    public void BigintLiteral() => Test(5, "arbitrary integer =", "32576516816534385321687165416384384381261681681", Scalar.bigint);
}
