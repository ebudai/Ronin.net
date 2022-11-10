using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal class Terminal : Punctuation
{
    internal Terminal(Lexer lexer, int length) : base(lexer, length) { }
}
