using Ronin.Parser;

namespace Unit;

public class BinaryLiteral : LiteralUnitTest
{
    public BinaryLiteral() : base("literals\\binary") { }

    [Fact(DisplayName = "parse binary literal")]
    public void Normal() => Test(0, "normal binary number =", "101", Scalar.@byte);

    [Fact(DisplayName = "parse separated binary literal")]
    public void Separated() => Test(1, "binary number with separators =", "100010100100", Scalar.bits16);
    
    [Fact(DisplayName = "parse 32-bit binary literal")]
    public void Binary32() => Test(2, "binary double word =", "110000000000000011110101", Scalar.bits32);

    [Fact(DisplayName = "parse 64-bit binary literal")]
    public void Binary64() => Test(3, "binary quad word =", "1010101010010111001010010010000101111101010101000101", Scalar.bits64);

    [Fact(DisplayName = "parse arbitrary binary literal")]
    public void Bitlist() => Test(4, "arbitrarily large binary value =", "10101010100101110010100100100001011111010101010001001010101010010111001010010010000101111101010101000101010001010100010101000101", Scalar.bitlist);
}
