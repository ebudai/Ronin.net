using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Symbols;

internal class OpenAngleBracketSymbol : Symbol
{
    public override Regex[] Regexes { get; } = { new(@"^<", options) };
}
