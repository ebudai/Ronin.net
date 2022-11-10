using Ronin.Compiler;

namespace Ronin.Grammar.Errors;

internal class ExpectedTerminal : Error, IParsable
{
    public static Syntax Parse(ref Parser context) => Parse<ExpectedTerminal>(ref context);
}
