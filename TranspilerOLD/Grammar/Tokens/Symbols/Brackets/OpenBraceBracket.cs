using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Symbols.Brackets;

internal class OpenBraceBracket : OpenBracketSymbol<CloseBraceBracket>
{
    public override Regex[] Regexes { get; } = { new(@"^{", options) };
}
