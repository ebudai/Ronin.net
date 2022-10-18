using Ronin.Compiler;

namespace Ronin.Token.Keywords;

internal class Import : Keyword
{
    internal const string keyword = "import";

    internal Import(Lexer lexer) : base(lexer, keyword.Length) { }
}
