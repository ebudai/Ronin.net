using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language.Fundamental;

[ExcludeFromCodeCoverage]
internal class Number : Fundamental<float>
{
    private const string sourcecode = """
        datatype number = whole number and 
        {
            var epsilon => shared number = 1.4 * 10^-45;

            intrinsic shared function | (me => number) | => number { return float.Abs(this); }
            
            intrinsic shared function sine (x => number) => number { return float.Sin(x); }
            intrinsic shared function cosine (x => number) => number { return float.Cos(x); }
            intrinsic shared function tangent (x => number) => number { return float.Tan(x); }
            intrinsic shared function inverse hyperbolic cosine (x => number) => number { return float.Acosh(x); }
            intrinsic shared function inverse hyperbolic sine (x => number) => number { return float.Asinh(x); }
            intrinsic shared function inverse hyperbolic tangent (x => number) => number { return float.Atanh(x); }
            intrinsic shared function hyperbolic cosine(x => number)  => number { return float.Cosh(x); }
            intrinsic shared function hyperbolic sine (x => number) => number { return float.Sinh(x); }
            intrinsic shared function hyperbolic tangent (x => number) => number { return float.Tanh(x); }
            intrinsic shared function inverse sine (x => number) => number { return float.Asin(x); }
            intrinsic shared function inverse cosine (x => number) => number { return float.Acos(x); }
            intrinsic shared function inverse tangent (x => number) => number { return float.Atan(x); }
            intrinsic shared function inverse tangent (x => number) / (y => number) => number { return float.Atan2(x, y); }

            intrinsic function square root => number { return float.Sqrt(this); }
            intrinsic function cube root => number { return float.Cbrt(this); }
            intrinsic function nth root (root => number) => number { return float.RootN(this, root); }

            intrinsic function ceiling => whole number { return (long)float.Ceiling(this); }
            intrinsic function floor => whole number { return (long)float.Floor(this); }
            intrinsic function round => whole number { return (long)float.Round(this); }
            intrinsic function round to (decimal places => whole number) places => number { return float.Round(this, decimal_places); }
            intrinsic function clamp (minimum => number, maximum => number) => number { return float.Clamp(this, minimum, maximum); }
            intrinsic function truncate => whole number { return (long)float.Truncate(this); }

            intrinsic function hypotenuse (y => number) => number { return float.Hypot(this, y); }

            intrinsic function + (addend => number) => number { return this + addend; }
            intrinsic function - (subtractend => number) => number { return this - subtractend; }
            intrinsic function * (multiplicand => number) => number { return this * multiplicand; }
            intrinsic function * (multiplicand => number) + (addend => number) => number { return float.FusedMultiplyAdd(this, mulitplicand, addend); }
            intrinsic function + (addend => number) * (multiplicand => number) => number { return float.FusedMultiplyAdd(this, mulitplicand, addend); }
            intrinsic function - (subtractend => number) * (multiplicand => number) => number { return this - (subtractend * multiplicand); }
            intrinsic function + (addend => number) / (divisor => number) => number { return this + (addend / divisor); }
            intrinsic function - (subtractend => number) / (divisor => number) => number { return this - (subtractend / divisor); }
            intrinsic function / (divisor => number) => number { return this / divisor; }
            intrinsic function modulo (modulus => number) => number { return this % modulus; }
            intrinsic function ^ (exponent => number) => number { return Math.Pow(this, exponent); }
            intrinsic function + (addend => number) ^ (exponent => number) => number { return this + Math.Pow(addend, exponent); }
            intrinsic function - (subtractend => number) ^ (exponent => number) => number { return this - Math.Pow(subtractend, exponent); }
            intrinsic function * (multiplicand => number) ^ (exponent => number) => number { return this * Math.Pow(multiplicand, exponent); }
            intrinsic function / (divisor => number) ^ (exponent => number) => number { return this / Math.Pow(divisor, exponent); }

            intrinsic function * 2 ^ (exponent => number) => number { return float.ScaleB(this, exponent); }
            intrinsic function *2 ^ (exponent => number) => number { return float.ScaleB(this, exponent); }
            intrinsic function * 2^ (exponent => number) => number { return float.ScaleB(this, exponent); }
            intrinsic function *2^ (exponent => number) => number { return float.ScaleB(this, exponent); }

            intrinsic shared function log (x => number) base 2 as whole number => number { return float.ILogB(x); }
            intrinsic shared function log (x => number) => number { return float.Log10(x); }
            intrinsic shared function log (x => number) base (base => number) => { return float.Log(x, base); }
            intrinsic shared function ln (x => number) => number { return float.Log(x); }
            
            intrinsic function < (other => number) => maybe { return this < other; }
            intrinsic function <= (other => number) => maybe { return this <= other; }
            intrinsic function > (other => number) => maybe { return this > other; }
            intrinsic function >= (other => number) => maybe { return this >= other; }

            intrinsic function sign => whole number { return float.Sign(this); }

            intrinsic function is whole number => maybe { return float.IsInteger(this); }

            intrinsic function as text => text { return this.ToString(); }
            intrinsic function as whole number => whole number { return (long)this; }
            intrinsic function as maybe => maybe { return this >= -float.Epsilon && this <= float.Epsilon; }

            intrinsic shared function min(x => number, y => number) => number { return float.Min(x, y); }
            intrinsic shared function max(x => number, y => number) => number { return float.Max(x, y); }

            constant min = -3.40282346638528859 * 10^38;
            constant max = 3.40282346638528859 * 10^38;
        }
        """;
    public Number() : base("number") { }
}
