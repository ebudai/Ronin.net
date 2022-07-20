using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Symbols.Brackets;

internal class OpenAngleBracket : OpenBracketSymbol<CloseAngleBracket>
{
    public override Regex[] Regexes { get; } = { new(@"^<", options) };
}
