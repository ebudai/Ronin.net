using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Literals;

internal class NumericLiteral : Literal
{
    public override Regex[] Regexes { get; } =
    {
        new(@"^(?<Value>-?0[xX][\d_a-fA-F]+)", options),
        new(@"^(?<Value>-?0[bB][01_]+)", options),
        new(@"^(?<Value>-?[\d_]+[uU]?[lL]?)", options),
        new(@"^(?<Value>-?[\d_]+[.]?[\d_]*[fF])", options),
        new(@"^(?<Value>-?[\d_]+[.][\d_]*[dD]?)", options),
        new(@"^(?<Value>-?[\d_]+[dD])", options),
        new(@"^(?<Value>-?[\d_]+[.][\d_]+[mM])", options),
        new(@"^(?<Value>-?[\d_]+[mM])", options),
    };
}
