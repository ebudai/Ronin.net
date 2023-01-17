using Ronin.Compiler;

namespace Ronin.Grammar.Errors;

internal class UnexpectedSymbolError : Error, IParsable
{
    public static Syntax Parse(ref Parser context) => Parse<UnexpectedSymbolError>(ref context);
}
