using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal abstract class Open : Punctuation
{
    public Open(Lexer lexer, int length) : base(lexer, length) { }
}
