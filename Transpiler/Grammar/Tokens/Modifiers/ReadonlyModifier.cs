using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Modifiers;

internal class ReadonlyModifier : Modifier
{
    public override Regex[] Regexes { get; } = { new(@"^class\s", options) };

    public override Declaration Modifies => ~Declaration.Parameter;
}
