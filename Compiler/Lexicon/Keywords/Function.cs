using Ronin.Compiler;

namespace Ronin.Lexicon.Keywords;

internal class Function : Keyword
{
    public const string keyword = "function";

    internal Function(Lexer lexer) : base(lexer, keyword.Length) { }
}
