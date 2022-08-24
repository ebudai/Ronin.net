using Ronin.Parser;

namespace Unit;

public class HexLiteral : LiteralUnitTest
{
    public HexLiteral() : base("literals\\hex") { }

    [Fact(DisplayName = "parse hex literal")]
    public void Normal() => Test(0, "normal hex number =", "75AE2c", Scalar.bits32);

    [Fact(DisplayName = "parse separated hex literal")]
    public void Separated() => Test(1, "hex number with separators =", "4EE3", Scalar.bits16);

    [Fact(DisplayName = "parse small hex literal")]
    public void SmallHex() => Test(2, "small hex =", "F", Scalar.@byte);

    [Fact(DisplayName = "parse big hex literal")]
    public void BigHex() => Test(3, "big hex number =", "4cDEADBEEF3000",  Scalar.bits64);

    [Fact(DisplayName = "parse arbitrary hex literal")]
    public void Bitlist() => Test(4, "arbitrary hex number =", "1AAAAAAAAEeEEeEBBBBBBBBBdddDD00111666666667", Scalar.bitlist);
}
