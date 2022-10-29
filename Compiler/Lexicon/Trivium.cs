using Ronin.Compiler;

namespace Ronin.Lexicon;

internal abstract class Trivium : Token
{
    protected internal Trivium(Lexer lexer, int length) : base(lexer, length)
    {
    }
}
