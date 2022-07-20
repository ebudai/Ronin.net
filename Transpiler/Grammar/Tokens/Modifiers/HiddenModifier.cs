using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Modifiers;

internal class HiddenModifier : Modifier
{
    public override Regex[] Regexes { get; } = { new(@"^hidden\s", options) };

    public override Declaration Modifies => Declaration.Type | Declaration.Member;
}
