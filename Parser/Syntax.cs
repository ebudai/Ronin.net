using System.Text.RegularExpressions;

namespace Ronin.Parser;

internal abstract class Syntax
{
    internal static readonly Regex whitespace = new(@"\s+", options);
    internal static readonly Regex textliteral = new(@"""[^""\\]*(\\.[^""\\]*)*""", options);
    internal static readonly Regex charliteral = new(@"'\\?.'", options);
    internal static readonly Regex unicodeliteral = new(@"'\\u[a-f0-9]{4}'", options | RegexOptions.IgnoreCase);
    internal static readonly Regex hexliteral = new(@"-?0x[\d_a-f]+", options | RegexOptions.IgnoreCase);
    internal static readonly Regex binaryliteral = new(@"0b[01_]+", options | RegexOptions.IgnoreCase);
    internal static readonly Regex decimalliteral = new(@"-?\d[\d_]*[.][\d_]+", options);
    internal static readonly Regex halfliteral = new(@"-?\d[\d_]*([.][\d_])?[\d_]*r16", options | RegexOptions.IgnoreCase);
    internal static readonly Regex doubleliteral = new(@"-?\d[\d_]*([.][\d_])?[\d_]*r64", options | RegexOptions.IgnoreCase);
    internal static readonly Regex moneyliteral = new(@"-?\$\d[\d_]*([.][\d_])?[\d_]*", options);
    internal static readonly Regex integerliteral = new(@"-?\d[\d_]*(i8|i16|i64)?", options | RegexOptions.IgnoreCase); //TODO take care of the suffix using units
    internal static readonly Regex symbol = new(@"[{(\[.,)\]}]", options);
    internal static readonly Regex keyword = new("function|datatype|var", options);
    internal static readonly Regex identifier = new(@"[^\d\s({\[\]}),.""][^\s({\[\]}),.""]*", options | RegexOptions.IgnoreCase);

    internal const string terminal = ".";
    internal const string separator = ",";

    internal const string scopeopen = "{";
    internal const string scopeclose = "}";

    internal const string groupingopen = "(";
    internal const string groupingclose = ")";

    internal const string listopen = "[";
    internal const string listclose = "]";

    internal const string binaryprefix = "0b";
    internal const string hexprefix = "0x";

    private const RegexOptions options = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;
}