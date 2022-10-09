using Ronin.Compiler;
using Ronin.Token;

namespace Ronin.Grammar;

internal class Value : Syntax, IParsable//<Value>
{
    internal Literal Literal { get; set; } //TODO what about date and then time?  needs to be supported, along with suffixes (like seconds, meters)

    internal Value(Parser parser, int length) : base(parser, length) { }

    public static Syntax Parse(ref Parser parser)
    {
        if (parser.IsEmpty) return null;

        if (parser[0] is not Token.Literal) return null;

        return new Value(parser, 1);
    }

    public string Transpile() => Literal.ToString();
}
