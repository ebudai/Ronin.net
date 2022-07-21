using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens;

internal class Whitespace : Token
{
    public string Spaces = string.Empty;

    public override Regex[] Regexes { get; } = { new(@"^(?<Spaces>\s+)", options) };
}
