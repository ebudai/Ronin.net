using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Symbols;

internal class TerminalSymbol : Symbol
{
    public override Regex[] Regexes { get; } = { new("^;", options) };
}
