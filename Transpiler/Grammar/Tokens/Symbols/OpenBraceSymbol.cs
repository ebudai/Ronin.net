using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Symbols;

internal class OpenBraceSymbol : Symbol
{
    public override Regex[] Regexes { get; } = { new(@"^{", options) };
}
