using System.Text.RegularExpressions;

namespace Ronin.Parser;
//TODO url literal
//TODO interpolated text literals
//TODO comments
public class Form
{
    internal static readonly Regex whitespace = new(@"\s+", options);
    internal static readonly Regex textliteral = new(@"""[^""\\]*(\\.[^""\\]*)*""", options);
    internal static readonly Regex charliteral = new(@"'\\?.'", options);
    internal static readonly Regex unicodeliteral = new(@"'\\u[\da-f]{4}'", options | RegexOptions.IgnoreCase);
    internal static readonly Regex hexliteral = new(@"0x[\d_a-f]+", options | RegexOptions.IgnoreCase);
    internal static readonly Regex binaryliteral = new(@"0b[01_]+", options | RegexOptions.IgnoreCase);
    internal static readonly Regex numberliteral = new(@"\d[\d_]*[.][\d_]+", options);
    internal static readonly Regex moneyliteral = new(@"\$\d[\d_]*([.][\d_])?[\d_]*", options);
    internal static readonly Regex integerliteral = new(@"\d[\d_]*\s*(i8|i16|i64)?", options | RegexOptions.IgnoreCase); //TODO take care of the suffix using units
    internal static readonly Regex symbol = new(@"[{(\[.,)\]}]", options);
    internal static readonly Regex declaration = new(@"(function|datatype|var|constant)\s*", options);
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