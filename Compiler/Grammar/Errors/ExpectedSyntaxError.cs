using Ronin.Compiler;
using Ronin.Lexicon;

namespace Ronin.Grammar.Errors;

internal class ExpectedSyntaxError<TSeparator, TClose> : Error 
    where TSeparator : Symbol
    where TClose : Symbol
{
    public ExpectedSyntaxError(ref Parser parser) : base(ref parser) { }
}
