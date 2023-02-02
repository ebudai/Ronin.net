using Ronin.Compiler;

namespace Ronin.Lexicon.Keywords;

internal class Reactive : Keyword
{
    public const string keyword = "reactive";

    internal Reactive(Lexer lexer) : base(lexer, keyword.Length) { }
}
