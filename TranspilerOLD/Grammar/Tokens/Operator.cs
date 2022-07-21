namespace Ronin.Transpiler.Grammar.Tokens;

internal abstract class Operator : Token, IComparable<Operator>
{
    protected internal abstract Precedence Precedence { get; }

    public int CompareTo(Operator other) => Precedence.CompareTo(other.Precedence);

    public static Operator[] GetOperators() => typeof(Operator).Assembly.DefinedTypes
        .Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(Operator)))
        .Select(type => Activator.CreateInstance(type) as Operator)
        .OrderBy(instance => instance.Precedence)
        .ToArray();
}

internal enum Precedence 
{
    Primary,
    Unary,
    Range,
    SwitchAndWith,
    Multiplicative,
    Additive,
    Shift,
    RelationalAndTypeTesting,
    Equality,
    BitwiseAnd,
    BitwiseXor,
    BitwiseOr,
    LogicalAnd,
    LogicalOr,
    NullCoalescing,
    Conditional,
    AssignmentAndLambda
}
/*
 * from https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/
 * 
 Operators	Category or name
0	x.y, f(x), a[i], x?.y, x?[y], x++, x--, x!, new, typeof, checked, unchecked, default, nameof, delegate, sizeof, stackalloc, x->y	Primary
1	+x, -x, !x, ~x, ++x, --x, ^x, (T)x, await, &x, *x, true and false	Unary
2	x..y	Range
3	switch, with	switch and with expressions
4	x * y, x / y, x % y	Multiplicative
5	x + y, x – y	Additive
6	x << y, x >> y, x >>> y	Shift
7	x < y, x > y, x <= y, x >= y, is, as	Relational and type-testing
8	x == y, x != y	Equality
9	x & y	Boolean logical AND or bitwise logical AND
10	x ^ y	Boolean logical XOR or bitwise logical XOR
11	x | y	Boolean logical OR or bitwise logical OR
12	x && y	Conditional AND
13	x || y	Conditional OR
14	x ?? y	Null-coalescing operator
15	c ? t : f	Conditional operator
16	x = y, x += y, x -= y, x *= y, x /= y, x %= y, x &= y, x |= y, x ^= y, x <<= y, x >>= y, x >>>= y, x ??= y, =>	Assignment and lambda declaration
 */