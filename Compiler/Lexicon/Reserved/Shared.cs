using Ronin.Compiler;

namespace Ronin.Lexicon.Reserved;

internal class Shared : Keyword
{
    internal const string keyword = "shared";

    internal Shared(Lexer lexer) : base(lexer, keyword.Length) { }
}
