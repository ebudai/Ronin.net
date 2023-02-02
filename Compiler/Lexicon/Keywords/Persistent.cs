using Ronin.Compiler;

namespace Ronin.Lexicon.Keywords;

internal class Persistent : Keyword
{
    public const string keyword = "persistent";

    internal Persistent(Lexer lexer) : base(lexer, keyword.Length) { }
}
