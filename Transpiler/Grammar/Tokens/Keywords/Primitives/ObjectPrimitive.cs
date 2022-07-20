using System.Text.RegularExpressions;

namespace Ronin.Transpiler.Grammar.Tokens.Keywords.Primitives;

internal class ObjectPrimitive : Primitive
{
    public override LexicalScope Applies => LexicalScope.All;

    public override Regex[] Regexes { get; } = { new(@"^object\s", options) };
}
