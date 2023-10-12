using Ronin.Grammar;

namespace Ronin.Semantics;

internal partial class Analyzer
{
    public void Types(Scope scope = null)
    {
        scope ??= Global;

        foreach (var statement in scope)
        {
            if (statement is Datum datum)
            {
                Types(datum.Identifier, scope);
                if (datum.Type is Type.Unresolved unresolved)
                {
                    var member = scope.Find(unresolved.Reference);
                    datum.Type = member as Type ?? new Type.Calculated { Member = member };
                }                
            }
            else if (statement is Function function and { Returns: Type.Unresolved returns })
            {
                var member = scope.Find(returns.Reference);
                function.Returns = member as Type ?? new Type.Calculated { Member = member };
                Types(function.Definition);
            }
        }
    }

    private static void Types(Identifier identifier, Scope scope)
    {
        foreach (var component in identifier)
        {
            if (component.IsT0) continue;
            Types(component.AsT1, scope);
        }
    }

    private static void Types(Parameters parameters, Scope scope)
    {
        foreach (var parameter in parameters)
        {

        }
    }
}
