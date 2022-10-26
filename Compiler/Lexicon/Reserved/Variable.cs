using Ronin.Compiler;

namespace Ronin.Lexicon.Reserved;

internal class Variable : Keyword
{
    internal const string keyword = "var";

    internal Variable(Lexer lexer) : base(lexer, keyword.Length) { }
}
