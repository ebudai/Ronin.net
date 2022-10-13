using Ronin.Compiler;

namespace Ronin.Grammar.Aggregate;

internal class Lookup : Syntax, IParsable
{
    public Lookup(Parser parser, int length) : base(parser, length)
    {
    }

    public static Syntax Parse(ref Parser parser)
    {
        throw new NotImplementedException();
    }
}
