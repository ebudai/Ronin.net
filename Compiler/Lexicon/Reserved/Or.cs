using Ronin.Compiler;

namespace Ronin.Lexicon.Reserved;

internal class Or : Keyword
{
    internal const string or = "or";

    internal Or(Lexer lexer) : base(lexer, or.Length) { }
}
