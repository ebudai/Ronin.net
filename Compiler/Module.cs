using Ronin.Grammar;
using Ronin.Grammar.Compound;
using Ronin.Language;

namespace Ronin.Compiler;

internal class Module
{
    public static Module Main = null;

    public Definition Definition { get; init; }
    

    public List<Error> Add(Name name, Module module)
    {
        throw new NotImplementedException();
    }

    public class Unresolved : Module
    {
        public Import Import { get; init; }
    }
}
