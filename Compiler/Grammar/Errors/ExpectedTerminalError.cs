using Ronin.Compiler;

namespace Ronin.Grammar.Errors;

internal class ExpectedTerminalError : Error, IParsable
{
    public static Syntax Parse(ref Parser context) => Parse<ExpectedTerminalError>(ref context);
}
