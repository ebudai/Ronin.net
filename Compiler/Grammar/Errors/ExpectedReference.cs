using Ronin.Compiler;

namespace Ronin.Grammar.Errors;

internal class ExpectedReference : Error, IParsable
{
    public static Syntax Parse(ref Parser context) => Parse<ExpectedReference>(ref context);
}
