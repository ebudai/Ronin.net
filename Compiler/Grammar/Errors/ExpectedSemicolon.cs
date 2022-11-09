using Ronin.Compiler;

namespace Ronin.Grammar.Errors;

internal class ExpectedSemicolon : Error, IParsable
{
    public static Syntax Parse(ref Parser context) => Parse<ExpectedSemicolon>(ref context);
}
