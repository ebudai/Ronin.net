using Ronin.Compiler;
using Ronin.Token;

namespace Ronin.Grammar;

internal class Expected : Syntax
{
    protected internal Expected(Parser parser) : base(parser, 0) { }
}

internal class Expected<T> : Expected where T : Lexeme
{
    protected internal Expected(Parser parser) : base(parser) { }
}

internal class Expected<T0, T1> : Expected where T0 : Lexeme where T1 : Lexeme
{
    protected internal Expected(Parser parser) : base(parser) { }
}

internal class Expected<T0, T1, T2> : Expected where T0 : Lexeme where T1 : Lexeme where T2 : Lexeme
{
    protected internal Expected(Parser parser) : base(parser) { }
}

internal class Expected<T0, T1, T2, T3> : Expected where T0 : Lexeme where T1 : Lexeme where T2 : Lexeme where T3 : Lexeme
{
    protected internal Expected(Parser parser) : base(parser) { }
}

internal class Expected<T0, T1, T2, T3, T4> : Expected where T0 : Lexeme where T1 : Lexeme where T2 : Lexeme where T3 : Lexeme where T4 : Lexeme
{
    protected internal Expected(Parser parser) : base(parser) { }
}

