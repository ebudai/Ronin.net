using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Keywords;

internal class IncludeKeyword : Keyword
{
    public override Regex[] Regexes { get; } = { new(@"^include\s", options) };
}
