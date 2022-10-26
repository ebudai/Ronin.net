using Ronin.Compiler;

namespace Ronin.Lexicon.Reserved;

internal class Optional : Keyword
{
    internal const string keyword = "optional";

    internal Optional(Lexer lexer) : base(lexer, keyword.Length) { }
}
