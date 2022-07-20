using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Keywords;

internal class OptionalKeyword : Keyword
{
    public override LexicalScope Applies => LexicalScope.All;

    public override Regex[] Regexes { get; } = { new(@"^optional\s", options) };
}
