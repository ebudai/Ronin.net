using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Symbols.Brackets;

internal class CloseAngleBracket : CloseBracketSymbol
{
    public override Regex[] Regexes { get; } = { new(@"^>", options) };
}
