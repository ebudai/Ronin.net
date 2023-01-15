using Ronin.Compiler;
using Ronin.Grammar.Unions;
using Ronin.Lexicon.Symbols;

namespace Ronin.Grammar;

internal class Scope : AggregateSyntax<Scope, OpenBrace, Statement, Terminal, CloseBrace>
{
    public static Scope Global;

    static Scope()
    {
        Global = new();

    }
}
