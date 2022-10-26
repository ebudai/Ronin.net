using Ronin.Compiler;

namespace Ronin.Lexicon.Reserved;

internal class Compiled : Keyword
{
    internal const string keyword = "compiled";

    public Compiled(Lexer lexer) : base(lexer, keyword.Length) { }
}
