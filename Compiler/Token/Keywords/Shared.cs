using Ronin.Compiler;

namespace Ronin.Token.Keywords;

internal class Shared : Keyword
{
    internal const string keyword = "shared";

    internal Shared(Lexer lexer) : base(lexer, keyword.Length) { }
}
