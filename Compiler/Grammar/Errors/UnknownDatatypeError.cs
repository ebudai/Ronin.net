using Ronin.Compiler;

namespace Ronin.Grammar.Errors;

internal class UnknownDatatypeError : Error, IParsable
{
    public static Syntax Parse(ref Parser context) => Parse<UnknownDatatypeError>(ref context);
}
