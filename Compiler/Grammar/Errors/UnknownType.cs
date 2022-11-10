using Ronin.Compiler;

namespace Ronin.Grammar.Errors;

internal class UnknownType : Error, IParsable
{
    public static Syntax Parse(ref Parser context) => Parse<UnknownType>(ref context);
}
