using Ronin.Compiler;
using Ronin.Token;

namespace Ronin.Grammar;

internal class Expected<T> : Syntax where T : Lexeme
{
    protected internal string[] Specifics;

    internal Expected(Parser parser, params string[] specifics) : base(parser, 1) => Specifics = specifics;    
}

internal class Expected<T0, T1> : Expected<T0> where T0 : Lexeme where T1 : Lexeme
{
    internal Expected(Parser parser, params string[] specifics) : base(parser, specifics) { }
}
