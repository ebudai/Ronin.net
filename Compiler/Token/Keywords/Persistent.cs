using Ronin.Compiler;

namespace Ronin.Token.Keywords;

internal class Persistent : Keyword
{
    internal const string keyword = "persistent";

    internal Persistent(Lexer lexer) : base(lexer, keyword.Length) { }
}
