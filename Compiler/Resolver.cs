using Ronin.Grammar;
using Ronin.Hierarchy;

using Function = Ronin.Grammar.Function;

namespace Ronin.Compiler;

internal static partial class Analyzer
{
    public static void Resolve(Context definition, List<Error> errors)
    {
        for (int i = 0, max = definition.Imports.Count; i != max; ++i)
        {
            if (definition.Imports[i] is not Context.Unresolved unresolved) continue;

            var module = Global.Scope.Get(unresolved.Import.Name);
            if (module is null)
            {
                errors.Add(Error.UnresolvedImport(unresolved.Import));
                continue;
            }
            definition.Imports[i] = module;
        }

        foreach (var name in definition.Members.Keys)
        {
            if (name.value is Parameters parameters)
            {
                foreach (var datum in parameters.Data.Values)
                {
                    if (datum.Datatype is not Datatype.Unresolved unresolved) continue;
                    datum.Datatype = Resolve(unresolved, definition, errors);
                }
            }

            var member = definition.Members[name];
            if (member is Datatype.Unresolved datatype)
            {
                definition.Members[name] = Resolve(datatype, definition, errors);
            }
            else if (member is Datum.Unresolved datum)
            {
                var resolved = definition.Resolve(datum.Reference);
                if (resolved.Count is 0)
                {
                    errors.Add(Error.CouldNotResolve(member, datum.Reference));
                    continue;
                }
                definition.Members[name] = resolved[0].Member;
            }
            else if (member is Datum and { Datatype: Datatype.Unresolved unresolved })
            {
                (member as Datum).Datatype = Resolve(unresolved, definition, errors);
            }
        }

        foreach (var child in definition.Children.Values)
        {
            Resolve(child, errors);
        }

        foreach (var statement in definition)
        {
            if (statement is Function.Call call and { Function: Function.Unresolved function })
            {
                var resolved = definition.Resolve(function.Reference);
                if (resolved.Count is 0)
                {
                    errors.Add(Error.CouldNotResolve(function, function.Reference));
                    continue;
                }
                call.Function = new Function.Overloaded
                {
                    Overloads = resolved,
                    Definition = function.Definition,
                    Modifiers = function.Modifiers,
                    Returns = function.Returns
                };
            }
        }
    }

    private static Datatype.Overloaded Resolve(Datatype.Unresolved datatype, Context definition, List<Error> errors)
    {
        var overloads = GetOverloads(definition, datatype, errors);
        var algebra = GetOverloadedAlgebra(definition, datatype.Algebra, errors);
        return new Datatype.Overloaded
        {
            Overloads = overloads,
            Algebra = algebra,
            Definition = datatype.Definition,
            Modifiers = datatype.Modifiers
        };
    }

    private static List<Resolution> GetOverloads(Context definition, Datatype.Unresolved datatype, List<Error> errors)
    {
        var overloads = definition.Resolve(datatype.Reference);
        if (overloads.Count is 0)
        {
            errors.Add(Error.CouldNotResolve(datatype, datatype.Reference));
            return overloads;
        }
        return overloads;
    }

    private static Algebra.Overloaded GetOverloadedAlgebra(Context definition, Algebra algebra, List<Error> errors)
    {
        if (algebra is not Algebra.Unresolved unresolved) return null;
        var overloads = definition.Resolve(unresolved.Reference);
        if (overloads.Count is 0)
        {
            errors.Add(Error.CouldNotResolve(algebra, unresolved.Reference));
        }
        return new Algebra.Overloaded { Overloads = overloads, Source = algebra.Source };
    }
}
