using Ronin.Grammar;

namespace Ronin.Compiler;

internal class Module : Definition
{
    public static Module Main = new();

    public Module Find(Name name) => Find(name.Source);

    public void Add(Name name, Definition definition, List<Error> errors)
    {
        
    }

    public new class Unresolved : Module
    {
        public Import Import { get; init; }
    }
}
