using Ronin.Grammar;

namespace Ronin.Semantics;

internal partial class Analyzer
{
    public void Types(IContext context = null)
    {
        context ??= Global;

        foreach (var statement in context)
        {
            if (statement is Datum datum)
            {
                Types(datum.Identifier, context);
                if (datum.Type is Type.Unresolved unresolved)
                {
                    var member = context.Resolve(unresolved.Reference);
                    datum.Type = member as Type ?? new Type.Calculated { Member = member };
                }                
            }
            else if (statement is Function function and { Returns: Type.Unresolved returns })
            {
                var member = context.Resolve(returns.Reference);
                function.Returns = member as Type ?? new Type.Calculated { Member = member };
                Types(function.Definition);
            }
        }
    }

    private static void Types(Identifier identifier, IContext context)
    {
        foreach (var component in identifier)
        {
            Types(component.AsParameters, context);
        }
    }

    private static void Types(Parameters parameters, IContext context)
    {
        if (parameters is null) return;
        foreach (var parameter in parameters)
        {

        }
    }
}
