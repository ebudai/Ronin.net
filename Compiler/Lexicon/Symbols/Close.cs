using Ronin.Compiler;

namespace Ronin.Lexicon.Symbols;

internal abstract class Close : Punctuation
{
    public Close(Lexer lexer, int length) : base(lexer, length) { }
}
