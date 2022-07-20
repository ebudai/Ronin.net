using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Keywords;

internal class FunctionKeyword : Keyword
{
    public override LexicalScope Applies => LexicalScope.All;

    public override Regex[] Regexes { get; } = { new(@"^function\s", options) };
}
