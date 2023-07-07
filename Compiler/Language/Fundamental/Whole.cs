namespace Ronin.Language.Fundamental;

internal class Whole : Fundamental<long>
{
    private const string sourcecode = """
        datatype whole number =
        {
            intrinsic shared function | (me => whole number) | => whole number { return long.Abs(this); }
            
            intrinsic function clamp (low => whole number, high => whole number) => whole number { return long.Clamp(this, low, high); }

            intrinsic function + (addend => whole number) => whole number { return this + addend; }
            intrinsic function - (subtractend => whole number) => whole number { return this - subtractend; }
            intrinsic function * (multiplicand => whole number) => whole number { return this * multiplicand; }
            intrinsic function * (multiplicand => whole number) + (addend => whole number) => whole number { return float.FusedMultiplyAdd(this, mulitplicand, addend); }
            intrinsic function + (addend => whole number) * (multiplicand => whole number) => whole number { return this + (addend * multiplicand); }
            intrinsic function - (subtractend => whole number) * (multiplicand => whole number) => whole number { return this - (subtractend * multiplicand); }
            intrinsic function + (addend => whole number) / (divisor => whole number) => whole number { return this + (addend / divisor); }
            intrinsic function - (subtractend => whole number) / (divisor => whole number) => whole number { return this - (subtractend / divisor); }
            intrinsic function / (divisor => whole number) => whole number { return this / divisor; }
            intrinsic function / (divisor => whole number) remainder => whole number { return long.DivRem(this, divisor); }
            intrinsic function ^ (exponent => whole number) => whole number { return Math.Pow(this, exponent); }
            intrinsic function + (addend => whole number) ^ (exponent => whole number) => whole number { return this + Math.Pow(addend, exponent); }
            intrinsic function - (subtractend => whole number) ^ (exponent => whole number) => whole number { return this - Math.Pow(subtractend, exponent); }
            intrinsic function * (multiplicand => whole number) ^ (exponent => whole number) => whole number { return this * Math.Pow(multiplicand, exponent); }
            intrinsic function / (divisor => whole number) ^ (exponent => whole number) => whole number { return this / Math.Pow(divisor, exponent); }

            intrinsic function modulo (modulus => whole number) => whole number { return this % modulus; }

            intrinsic function is power of 2 => maybe { return long.IsPow2(this); }

            intrinsic shared function min (x => whole number, y => whole number) => whole number { return long.Min(x, y); }
            intrinsic shared function max (x => whole number, y => whole number) => whole number { return long.Max(x, y); }        

            intrinsic function count leading zeroes => whole number { return long.LeadingZeroCount(this); }
            intrinsic function count trailing zeroes => whole number { return long.TrailingZeroCount(this); }

            intrinsic function as text => text { return this.ToString(); }
            intrinsic function as maybe => maybe { return this != 0; }
        }
        """;

    public Whole() : base("whole number") { }
}
