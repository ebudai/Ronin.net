using Ronin.Compiler;

namespace Ronin.Grammar.Errors;

internal class ExpectedDatatypeOrInitializer : Error, IParsable
{
    public static Syntax Parse(ref Parser context) => Parse<ExpectedDatatypeOrInitializer>(ref context);
}
