using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Literals;

internal class CharLiteral : Literal
{
    public override Regex[] Regexes { get; } = { new(@"^'(?<Value>\\?.)'", options), new(@"^'(?<Value>\\[uU][a-fA-F0-9]{4})'", options) };
}
