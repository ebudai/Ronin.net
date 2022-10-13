using Ronin.Compiler;

namespace Ronin.Grammar.Aggregate;

internal class List : Syntax, IParsable
{
    public List(Parser parser, int length) : base(parser, length) { }

    public static Syntax Parse(ref Parser parser)
    {
        throw new NotImplementedException();
    }
}
