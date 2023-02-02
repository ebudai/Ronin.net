using Ronin.Compiler;

namespace Ronin.Lexicon.Keywords;

internal class Import : Keyword
{
    public const string keyword = "import";

    internal Import(Lexer lexer) : base(lexer, keyword.Length) { }
}
