using Ronin.Compiler;

namespace Ronin.Grammar.Errors;

internal class UnspecifiedDatatypeError : Error, IParsable
{
    public static Syntax Parse(ref Parser context) => Parse<UnspecifiedDatatypeError>(ref context);
}
