using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Literals;

internal class BooleanLiteral : Literal
{
    public override Regex[] Regexes { get; } = { new(@"^(?<Value>true|false)", options) };
}
