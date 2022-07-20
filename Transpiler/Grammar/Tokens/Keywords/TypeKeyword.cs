using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Keywords;

internal class TypeKeyword : Keyword
{
    public override Regex[] Regexes { get; } = { new(@"^type\s", options) };
}
