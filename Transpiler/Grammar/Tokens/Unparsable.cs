using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens;

internal class Unparsable : Token
{
    public override Regex[] Regexes { get; } = { new(@"^[^\s]+", options) };
}
