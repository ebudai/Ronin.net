using Ronin.Compiler;

namespace Ronin.Token.Keywords;

internal class PartOf : Keyword
{
    internal const string keyword = "part of";

    internal PartOf(Lexer lexer) : base(lexer, keyword.Length) { }
}
