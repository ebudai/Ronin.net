using Ronin.Compiler;
using Ronin.Token;

namespace Ronin.Grammar;

internal class Expected : Syntax
{
    internal Expected(Parser parser, params string[] specifics) : base(parser, 0) => Specifics = specifics;

    protected internal string[] Specifics;
}

internal class Expected<T> : Expected where T : Lexeme
{
    internal Expected(Parser parser, params string[] specifics) : base(parser, specifics) { }
}

internal class Expected<T0, T1> : Expected<T0> where T0 : Lexeme where T1 : Lexeme
{
    internal Expected(Parser parser, params string[] specifics) : base(parser, specifics) { }
}
