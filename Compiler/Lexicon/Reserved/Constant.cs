using Ronin.Compiler;

namespace Ronin.Lexicon.Reserved;

internal class Constant : Keyword
{
    internal const string keyword = "constant";

    public Constant(Lexer lexer) : base(lexer, keyword.Length) { }
}
