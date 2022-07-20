using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Symbols.Brackets;

internal class OpenSquareBracket : OpenBracketSymbol<CloseSquareBracket>
{
    public override Regex[] Regexes { get; } = { new(@"^\[", options) };
}
