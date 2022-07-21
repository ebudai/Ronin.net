using Ronin.Transpiler.Grammar.Flags;
using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Keywords;

internal class VarKeyword : Keyword
{
    public override LexicalScope Applies => LexicalScope.Global | LexicalScope.Function;

    public override Regex[] Regexes { get; } = { new(@"^var\s", options) };
}
