using Ronin.Compiler;

namespace Ronin.Grammar.Errors;

internal class ExpectedReferenceError : Error, IParsable
{
    public static Syntax Parse(ref Parser context) => Parse<ExpectedReferenceError>(ref context);
}
