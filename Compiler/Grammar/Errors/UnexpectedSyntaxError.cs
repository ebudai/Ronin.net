using Ronin.Compiler;

namespace Ronin.Grammar.Errors;

internal class UnexpectedSyntaxError : Error, IParsable
{
    public static Syntax Parse(ref Parser context) => Parse<UnexpectedSyntaxError>(ref context);
}
