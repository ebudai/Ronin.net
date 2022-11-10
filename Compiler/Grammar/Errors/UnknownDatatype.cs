using Ronin.Compiler;

namespace Ronin.Grammar.Errors;

internal class UnknownDatatype : Error, IParsable
{
    public static Syntax Parse(ref Parser context) => Parse<UnknownDatatype>(ref context);
}
