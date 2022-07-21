using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Literals;

internal class StringLiteral : Literal
{
    public override Regex[] Regexes { get; } = { new(@"^""(?<Value>.+?)[^\\]""", options) };
}
