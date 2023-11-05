using Ronin.Grammar;

namespace Ronin.Semantics;

internal partial class Analyzer
{
    public void Members(IContext context)
    {
        if (context is Module module)
        {
            ModuleMembers(module);
        }
        else if (context is Scope scope)
        {
            ScopeMembers(scope);
        }
    }

    private void ModuleMembers(Module module)
    {
        foreach (var scope in module.Scopes)
        {
            ScopeMembers(scope);
        }
        foreach (var submodule in module.Modules.Values)
        {
            ModuleMembers(submodule);
        }
    }

    private void ScopeMembers(Scope scope) 
    {
        for (var i = 0; i != scope.Statements.Count; ++i)
        {
            if (scope.Statements[i] is Association association) 
            {
                AssociationMembers(association, scope); 
            }
            else if (scope.Statements[i] is Value value)
            {
                scope.Statements[i] = ValueMembers(value, scope);
            }
            else if (scope.Statements[i] is Scope subscope)
            {
                ScopeMembers(subscope);
            }
        }
    }

    private static void AssociationMembers(Association association, IContext context)
    {
        association.Destination = ValueMembers(association.Destination, context);
        association.Origin = ValueMembers(association.Origin, context);
    }

    private static Value ValueMembers(Value value, IContext context) => value switch
    {
        Member.Unresolved member => context.Resolve(member.Reference),
        Datum.Unresolved datum => context.Resolve(datum.Reference),
        _ => value
    };
}