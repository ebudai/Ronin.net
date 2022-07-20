using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Keywords;

internal class IncludeKeyword : Keyword
{
    public override LexicalScope Applies => LexicalScope.Global;

    public override Regex[] Regexes { get; } = { new(@"^include\s", options) };
}
