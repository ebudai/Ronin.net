using Ronin.Compiler;

namespace Ronin.Grammar.Aggregate;

internal class Scope : Syntax, IParsable
{
    public Scope(Parser parser, int length) : base(parser, length) { }

    public static Syntax Parse(ref Parser parser)
    {
        throw new NotImplementedException();
    }
}
