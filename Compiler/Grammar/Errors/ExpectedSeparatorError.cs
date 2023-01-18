using Ronin.Compiler;

namespace Ronin.Grammar.Errors;

internal class ExpectedSeparatorError : Error, IParsable
{
    public static Syntax Parse(ref Parser context) => Parse<ExpectedSeparatorError>(ref context);
}
