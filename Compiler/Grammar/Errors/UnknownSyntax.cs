using Ronin.Compiler;

namespace Ronin.Grammar.Errors;

internal class UnknownSyntax : Error, IParsable
{
    public static Syntax Parse(ref Parser parser) => Parse<UnknownSyntax>(ref parser);
}
