using Ronin.Compiler;

namespace Ronin.Grammar.Declaration;

internal class Datatype : Syntax, IParsable//<Datatype>
{
    internal Datatype(Parser parser, int length) : base(parser, length) { }

    public static Syntax Parse(ref Parser parser)
    {
        throw new NotImplementedException();
    }

    public string Transpile()
    {
        throw new NotImplementedException();
    }
}
