using Ronin.Compiler;

namespace Ronin.Grammar;

internal class List : Syntax, IParsable
{
    public List(Parser parser, int length) : base(parser, length) { }

    public static Syntax Parse(Parser parser)
    {
        throw new NotImplementedException();
    }
}
