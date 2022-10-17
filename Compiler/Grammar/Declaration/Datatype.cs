using Ronin.Compiler;

namespace Ronin.Grammar.Declaration;

internal class Datatype : Syntax, IParsable
{
    internal Datatype(Parser parser, int length) : base(parser, length) { }

    public static Syntax Parse(Parser parser)
    {
        throw new NotImplementedException();
    }
}
