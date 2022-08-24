using Ronin.Parser;

namespace Unit;

public class CharacterLiteral : LiteralUnitTest
{
    public CharacterLiteral() : base("literals\\char") { }

    [Fact(DisplayName = "parse character literal")]
    public void Literal() => Test(0, "regular char =", "'c'", Scalar.character);

    [Fact(DisplayName = "parse unichar literal")]
    public void UnicharLiteral() => Test(1, "unichar =", @"'\u05E4'", Scalar.character);
}
