using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens;

internal class Identifier : Token
{
    public string Value = string.Empty;

    public override Regex[] Regexes { get; } = { new(@"^(?<Value>[A-Za-z][A-Za-z0-9_]*)", options) };
}
