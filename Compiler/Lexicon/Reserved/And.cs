using Ronin.Compiler;

namespace Ronin.Lexicon.Reserved;

internal class And : Keyword
{
    internal const string and = "and";

    internal And(Lexer lexer) : base(lexer, and.Length) { }
}
