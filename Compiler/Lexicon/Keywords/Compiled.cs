using Ronin.Compiler;

namespace Ronin.Lexicon.Keywords;

internal class Compiled : Keyword
{
    public const string keyword = "compiled";

    internal Compiled(Lexer lexer) : base(lexer, keyword.Length) { }
}
