using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Keywords;

internal class TypeKeyword : Keyword
{
    public override LexicalScope Applies => LexicalScope.Global | LexicalScope.TypeDefinition;

    public override Regex[] Regexes { get; } = { new(@"^type\s", options) };
}
