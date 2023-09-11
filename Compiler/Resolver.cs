using Ronin.Grammar;
using System;
using System.Collections.Generic;

namespace Ronin.Compiler;

internal abstract class Resolution
{
    public static Resolution From(List<Resolution> resolutions) => resolutions.Count switch
    {
        0 => null,
        1 => resolutions[0],
        _ => new Ambiguous { Candidates = resolutions }
    };

    public static Resolution Match(Context context, Identifier name, Reference reference)
    {
        throw new NotImplementedException();
    }

    public class Exact : Resolution
    {
        public Context.Member Member { get; set; }
        public List<Resolution> Inputs { get; } = new();
    }

    public class Ambiguous : Resolution
    {
        public List<Resolution> Candidates { get; init; }
    }
}

/*internal static partial class Analyzer
{
    public static void Resolve(Context definition, List<Error> errors)
    {
        Resolve(definition.Imports, errors);

        foreach (var name in definition.Members.Keys)
        {
            if (name.value is not Parameters parameters) continue;
            
            foreach (var datum in parameters.Data.Values)
            {
                if (datum.Datatype is not Datatype.Unresolved datatype) continue;
                datum.Datatype = Resolve(datatype, definition, errors);
            }
        }
        
        foreach (var (name, member) in definition.Members)
        {
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
            else if (member is Datum memberdatum and { Datatype: Datatype.Unresolved unresolved })
            {
                memberdatum.Datatype = Resolve(unresolved, definition, errors);
            }
            else if (member is Function function and { Returns: Datatype.Unresolved returns })
            {
                function.Returns = Resolve(returns, definition, errors);
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

    private static void Resolve(List<Context> imports, List<Error> errors)
    {
        for (int i = 0, max = imports.Count; i != max; ++i)
        {
            if (imports[i] is not Context.Unresolved unresolved) continue;

            var module = Global.Module.Get(unresolved.Import.Name);
            if (module is not null)
            {
                imports[i] = module;
                continue;
            }
            errors.Add(Error.UnresolvedImport(unresolved.Import));
        }
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
}*/
