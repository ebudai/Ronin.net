using Ronin.Grammar;
using System.Collections.Generic;

namespace Ronin.Hierarchy;

internal class Module : Context
{
    private readonly List<Context> Contexts = new();

    public void Add(Context context) => Contexts.Add(context);

    public override Resolution Find(Reference reference)
    {
        return base.Find(reference);
    }

    public new class Unresolved : Module
    {
        public Unresolved(Import import) => Import = import;

        public new Import Import { get; }
    }
}