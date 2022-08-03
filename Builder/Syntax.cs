using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Ronin.Builder;

internal interface ISyntax
{
    internal static virtual ISyntax
}
internal abstract class Syntax
{
    internal string Value { get; set; }
    internal abstract Regex Form { get; }

    protected internal const RegexOptions options = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Multiline;
}

internal class Symbol : Syntax
{
    internal override Regex Form { get; } = new(@"[,.]", options);
}

internal class Identifier : Syntax
{
    internal override Regex Form { get; } = new(@"[^\d\s({}),.""][^\s({}),.""]*", options);
}

internal class Whitespace : Syntax
{
    internal override Regex Form { get; } = new(@"\s+", options);
}

internal abstract class Literal : Syntax
{
    internal abstract string Datatype { get; }
}

internal class TextLiteral : Literal
{
    internal override string Datatype { get; } = Language.Primitives.text;
    internal override Regex Form { get; } = new(@"""[^""\\]*(\\.[^""\\]*)*""", options);
}

internal class CharacterLiteral : Literal
{
    internal override string Datatype { get; } = Language.Primitives.character;
    internal override Regex Form { get; } = new(@"'\\?.'", options);
}

internal class UnicharLiteral : Literal
{
    internal override string Datatype { get; } = Language.Primitives.character;
    internal override Regex Form { get; } = new(@"'\\u[a-f0-9]{4}'", options | RegexOptions.IgnoreCase);
}

internal abstract class NumericLiteral : Literal
{
    protected internal string SmallestIntType(NumberStyles style = NumberStyles.None)
    {
        style |= NumberStyles.AllowThousands;

        var success = BigInteger.TryParse(Value, style, CultureInfo.CurrentCulture, out var value);
        return !success ? null
            : value <= sbyte.MaxValue ? Language.Primitives.int8
            : value <= short.MaxValue ? Language.Primitives.int16
            : value <= int.MaxValue ? Language.Primitives.integer
            : value <= long.MaxValue ? Language.Primitives.int64
            : Language.Primitives.bigint;        
    }
}

internal class HexLiteral : NumericLiteral
{
    internal override string Datatype => SmallestIntType(NumberStyles.AllowHexSpecifier);
    internal override Regex Form { get; } = new(@"0x[\d_a-f]+", options | RegexOptions.IgnoreCase);
}

internal class BinaryLiteral : NumericLiteral
{
    internal override string Datatype => Value.Length switch
    {
        <= 8 => Language.Primitives.@byte,
        <= 16 => Language.Primitives.bits16,
        <= 32 => Language.Primitives.bits32,
        <= 64 => Language.Primitives.bits64,
        _ => Language.Primitives.bitlist
    };
    internal override Regex Form { get; } = new(@"0b[01_]+", options);
}

internal class DecimalLiteral : NumericLiteral
{
    internal override string Datatype { get; } = Language.Primitives.@decimal;
    internal override Regex Form { get; } = new(@"\d[\d_]*[.][\d_]", options);
}

internal class HalfPrecisionDecimalLiteral : NumericLiteral
{
    internal override string Datatype { get; } = Language.Primitives.dec16;
    internal override Regex Form { get; } = new(@"\d[\d_]*([.][\d_])?[\d_]*d16", options | RegexOptions.IgnoreCase);
}

internal class DoublePrecisionDecimalLiteral : NumericLiteral
{
    internal override string Datatype { get; } = Language.Primitives.dec64;
    internal override Regex Form { get; } = new(@"\d[\d_]*([.][\d_])?[\d_]*d64", options | RegexOptions.IgnoreCase);
}

internal class IntegerLiteral : NumericLiteral
{
    internal override string Datatype => SmallestIntType(NumberStyles.None);
    internal override Regex Form { get; } = new(@"\d[\d_]*(i8|i16|i64)?", options | RegexOptions.IgnoreCase);
}

internal class DateLiteral : Literal
{
    internal override string Datatype { get; } = Language.Primitives.date;
    internal override Regex Form { get; } = new(@"\d{4}-\d\d?-\d\d?", options);
}

internal class TimeLiteral : Literal
{
    internal override string Datatype { get; } = Language.Primitives.time;
    internal override Regex Form { get; } = new(@"\d\d?:\d\d(:\d\d)?\s?[ap]?[a-z]{3}?", options);
}

internal class DateTimeLiteral : Literal
{
    internal override string Datatype { get; } = Language.Primitives.datetime;
    internal override Regex Form { get; } = new(@"\d{4}-\d\d?-\d\d?\s\d\d?:\d\d(:\d\d)?\s?[ap]?[a-z]{3}?", options);
}

internal class MoneyLiteral : Literal
{
    internal override string Datatype { get; } = Language.Primitives.money;
    internal override Regex Form { get; } = new(@"\$\d[\d_]*([.][\d_])?[\d_]*", options);
}

internal class Instance : Syntax
{
    internal Scope Type { get; init; }
    internal Expression Initializer { get; init; }
    internal List<string> Modifiers { get; init; } = new();

    internal override Regex Form { get; } = new(@"", options);
}

internal class Union<T, U> : Syntax where T : Syntax where U : Syntax
{
    private Union() { }

    public static implicit operator Union<T, U>(T value) => new() { value = value };
    public static implicit operator Union<T, U>(U value) => new() { value = value };

    public static implicit operator T(Union<T, U> union) => union.value as T;
    public static implicit operator U(Union<T, U> union) => union.value as U;

    private object value;
}

internal abstract class Aggregate<T> : Syntax
{
    internal abstract string Start { get; }
    internal abstract string End { get; }
    internal abstract string Delimiter { get; }
    internal List<T> Members { get; } = new();
    internal sealed override Regex Form => form ??= new($@"\{Start}(?>\{Start}(?<c>)|[^\{Start}\{End}]+|\{End}(?<-c>))*(?(c)(?!))\{End}", options);

    private Regex form = null;
}

internal abstract class Aggregate<T, U> : Syntax where T : Syntax where U : Syntax
{
    internal abstract string Start { get; }
    internal abstract string End { get; }
    internal abstract string Delimiter { get; }
    internal List<Union<T, U>> Members { get; } = new();
    internal sealed override Regex Form => form ??= new($@"\{Start}(?>\{Start}(?<c>)|[^\{Start}\{End}]+|\{End}(?<-c>))*(?(c)(?!))\{End}", options);

    private Regex form = null;
}

internal class ObjectLiteral : Aggregate<Union<Literal, Identifier>>
{
    internal override string Start => Language.Symbols.Aggregates[0].ToString();
    internal override string End => Language.Symbols.Aggregates[1].ToString();
    internal override string Delimiter => Language.Symbols.Separator;
}

internal class CollectionLiteral : Aggregate<Union<Literal, Identifier>>
{
    internal override string Start => Language.Symbols.Lists[0].ToString();
    internal override string End => Language.Symbols.Lists[1].ToString();
    internal override string Delimiter => Language.Symbols.Separator;
}

internal class ParameterBlock : Aggregate<Instance>
{
    internal override string Start => Language.Symbols.Aggregates[0].ToString();
    internal override string End => Language.Symbols.Aggregates[1].ToString();
    internal override string Delimiter => Language.Symbols.Separator;
}

internal class Expression : Aggregate<Syntax>
{
    private Expression() { }

    internal string ConstantValue { get; set; }
    internal Identifier Functionall { get; set; }

    internal override string Start => string.Empty;
    internal override string End => string.Empty;
    internal override string Delimiter => Language.Symbols.Terminal;
}

internal class Scope : Aggregate<Expression>
{
    public static Scope Global { get; } = new(null);

    internal Scope(Scope parent) => Parent = parent;

    internal Scope Parent { get; set; }
    internal List<(Identifier name, Identifier datatype)> Parameters { get; } = new();

    internal sealed override string Start => Language.Symbols.Scopes[0].ToString();
    internal sealed override string End => Language.Symbols.Scopes[1].ToString();
    internal sealed override string Delimiter => Language.Symbols.Terminal;
    //internal override Regex Form { get; } = new(@"{(?>{(?<c>)|[^{}]+|}(?<-c>))*(?(c)(?!))}", options);

    /*
     * internal ScopeOld(ScopeOld parent) { Parent = parent; }

    internal bool IsDatatype => ReferenceEquals(ResolvesTo, this);

    internal ScopeOld Parent { get; set; }
    internal ScopeOld ResolvesTo { get; set; }

    internal int Column { get; set; }
    internal int Line { get; set; }
    internal string Filename { get; set; }

    internal Dictionary<Identifier, ScopeOld> Datatypes { get; init; } = new();
    internal Dictionary<Identifier, ScopeOld> Functions { get; init; } = new();
    internal Dictionary<Identifier, Instance> Parameters { get; init; } = new();
    internal Dictionary<Identifier, Instance> LocalData { get; init; } = new();
    internal List<ScopeOld> AnonymousScopes { get; init; } = new();
    internal List<Expression> FunctionCalls { get; init; } = new();
    internal List<string> Modifiers { get; init; } = new();

    internal static Scope Global { get; } = new(null);
     */
}