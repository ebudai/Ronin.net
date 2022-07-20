using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Symbols;

internal class OpenBracketSymbol : Symbol
{
    public override Regex[] Regexes { get; } = { new(@"^\(", options) };
}
