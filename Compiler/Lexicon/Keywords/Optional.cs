using Ronin.Compiler;

namespace Ronin.Lexicon.Keywords;

internal class Optional : Keyword
{
    public const string keyword = "optional";

    internal Optional(Lexer lexer) : base(lexer, keyword.Length) { }
}
