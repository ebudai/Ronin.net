using Ronin.Compiler;

namespace Ronin.Grammar;

internal class Reference : Syntax, IParsable<Reference>
{
    internal string Name { get; set; }

    internal Reference(Parser parser, int length) : base(parser, length) { }

    public static Syntax Parse(Parser parser)
    {
        throw new NotImplementedException();
    }

    public string Transpile()
    {
        throw new NotImplementedException();
    }
}
