using Ronin.Grammar;
using Ronin.Parser;

namespace Unit;

public class TextLiteral : LiteralUnitTest
{
    public TextLiteral() : base("literals\\text") { }

    [Fact(DisplayName = "parse text literal")]
    public void Literal() => Test(0, "normal text =", "\"regular text\"", Scalar.text);

    [Fact(DisplayName = "parse multiline text literal")]
    public void MultilineLiteral() => Test(1, "multiline text =", "\" this is" + Environment.NewLine + "\tmultiline with whitepsace\"", Scalar.text);

    [Fact(DisplayName = "parse text literal with embedded literals")]
    public void LiteralWithEmbeddedLiterals() => Test(2, "text with literals inside it =", "\"'c' is a char literal, 0xAAE is hex, 0b1101_0101 is binary\"", Scalar.text);
}
