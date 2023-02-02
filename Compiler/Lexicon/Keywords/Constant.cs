using Ronin.Compiler;

namespace Ronin.Lexicon.Keywords;

internal class Constant : Keyword
{
    public const string keyword = "constant";

    internal Constant(Lexer lexer) : base(lexer, keyword.Length) { }
}
