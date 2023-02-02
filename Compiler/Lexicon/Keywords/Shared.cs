using Ronin.Compiler;

namespace Ronin.Lexicon.Keywords;

internal class Shared : Keyword
{
    public const string keyword = "shared";

    internal Shared(Lexer lexer) : base(lexer, keyword.Length) { }
}
