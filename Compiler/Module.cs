using Ronin.Grammar;
using Ronin.Language;

namespace Ronin.Compiler;

internal class Module : AnonymousScope
{
    public static Module Main = null;
    
    public List<Error> Add(Name name, Module module)
    {
        throw new NotImplementedException();
    }

    public class Unresolved : Module
    {
        public Import Import { get; init; }
    }
}
