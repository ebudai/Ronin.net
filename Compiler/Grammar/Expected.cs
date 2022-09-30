using Ronin.Compiler;
using Ronin.Token;

namespace Ronin.Grammar;

internal class Expected<T> : Syntax where T : Lexeme
{
    internal Expected(Parser parser) : base(parser, 0)
    {
    
    }

}
