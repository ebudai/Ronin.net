using Ronin.Compiler;

namespace Ronin.Lexicon.Keywords;

internal class PartOf : Keyword
{
    public const string keyword = "part of";

    internal PartOf(Lexer lexer) : base(lexer, keyword.Length) { }
}
