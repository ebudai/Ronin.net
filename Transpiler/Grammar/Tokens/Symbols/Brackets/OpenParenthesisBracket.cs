using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Symbols.Brackets;

internal class OpenParenthesisBracket : OpenBracketSymbol<CloseParenthesisBracket>
{
    public override Regex[] Regexes { get; } = { new(@"^\(", options) };
}
