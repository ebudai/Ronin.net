using Ronin.Grammar;
using static Ronin.Grammar.Definition;
using System.Xml.Linq;

namespace Ronin.Compiler;

//TODO make this nonstatic
internal static class Analyzer
{
    public static void Define(Definition parent, Definition definition, List<Error> errors)
    {
        definition.Parent = parent;

        foreach (var statement in definition.Statements)
        {
            if (statement is Export export)
            {
                errors.Add(Error.ScopeMustBeAnonymous(definition, export));
                continue;
            }
            Define(definition, statement, errors);
        }
    }

    public static void Define(Definition parent, Scope scope, List<Error> errors)
    {
        scope.Definition.Parent = parent;
        
        Identifier name = null;
        
        foreach (var statement in scope.Definition.Values)
        {
            if (statement is Export export)
            {
                Export(scope, export, ref name, errors);
                continue;
            }
            Define(scope.Definition, statement, errors);
        }
        
        if (name is not null)
        {
            var module = Global.Scope.GetModule(name);
            scope.Definition.Join(module, errors);
        }
    }

    public static void Resolve(Definition definition, List<Error> errors)
    {
        for (int i = 0, max = definition.Imports.Count; i != max; ++i)
        {
            if (definition.Imports[i] is not Unresolved unresolved) continue;

            var module = Global.Scope.GetModule(unresolved.Import.Name);
            if (module is null)
            {
                errors.Add(Error.UnresolvedImport(unresolved.Import));
                continue;
            }
            definition.Imports[i] = module;
        }

        foreach (var (name, member) in definition.Members)
        {
            if (member is Datatype.Unresolved datatype)
            {
                var overloads = GetOverloads(definition, member, datatype, errors);
                definition.Members[name] = new Datatype.Overloaded
                {
                    Overloads = overloads,
                    Algebra = datatype.Algebra,
                    Definition = datatype.Definition,
                    Modifiers = datatype.Modifiers
                };
            }
            else if (member is Datum.Unresolved unresolved)
            {
                var resolved = definition.Find(unresolved.Reference);
                if (resolved.Count is 0)
                {
                    errors.Add(Error.CouldNotResolve(member, unresolved.Reference));
                    continue;
                }
                definition.Members[name] = resolved[0];
            }
            else if (member is Datum datum and { Datatype: Datatype.Unresolved unresolvedDatatype })
            {
                var overloads = GetOverloads(definition, datum, unresolvedDatatype, errors);
                datum.Datatype = new Datatype.Overloaded
                {
                    Overloads = overloads,
                    Algebra = unresolvedDatatype.Algebra,
                    Definition = unresolvedDatatype.Definition,
                    Modifiers = unresolvedDatatype.Modifiers
                };
            }
        }

        foreach (var child in definition.Children.Values)
        {
            Resolve(child, errors);
        }

        foreach (var statement in definition.Statements)
        {
            if (statement is Function.Call call and { Function: Function.Unresolved function })
            {
                var resolved = definition.Find(function.Reference);
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

    private static List<Member> GetOverloads(Definition definition, Member member, Datatype.Unresolved datatype, List<Error> errors)
    {
        var overloads = definition.Find(datatype.Reference);
        if (overloads.Count is 0)
        {
            errors.Add(Error.CouldNotResolve(member, datatype.Reference));
            return overloads;
        }

        if (datatype.Algebra is Algebra.Unresolved unresolved)
        {
            var algebra = definition.Find(unresolved.Reference);
            datatype.Algebra = new Algebra.Overloaded { Overloads = algebra };
        }

        return overloads;
    }

    private static void Define(Definition definition, Statement statement, List<Error> errors)
    {
        switch (statement)
        {
            case Import import: Import(definition, import); break;
            case Function.Declaration function: Define(definition, function, errors); break;
            case Datatype.Declaration datatype: Define(definition, datatype, errors); break;
            case Datum.Declaration datum: Define(definition, datum, errors); break;
            case Scope inner: Define(definition, inner, errors); break;
            case Unknown unknown: errors.Add(Error.UnknownSyntax(unknown)); break;
            default: break;
        }
    }

    private static void Export(Scope scope, Export export, ref Identifier identifier, List<Error> errors)
    {
        bool error = false;

        if (scope is not AnonymousScope)
        {
            errors.Add(Error.ScopeMustBeAnonymous(scope.Definition, export));
            error = true;
        }

        if (scope.Modifiers.Source.IsEmpty is false)
        {
            errors.Add(Error.ScopeMustBeUnmodified(scope.Definition, export));
            error = true;
        }

        if (identifier is not null)
        {
            errors.Add(Error.ScopeIsAlreadyPartOfAModule(scope.Definition, export));
            error = true;
        }
        
        if (error is false) identifier = export.Identifier;
    }

    private static void Import(Definition definition, Import import)
    {
        definition.Imports.Add(new Definition.Unresolved { Import = import });
    }

    private static void Define(Definition definition, Function.Declaration declaration, List<Error> errors)
    {
        Define(definition, declaration.Definition, errors);
        
        Function function = new()
        {
            Modifiers = declaration.Modifiers,
            Returns = new Datatype.Unresolved { Reference = declaration.Returns },
            Definition = declaration.Definition
        };

        definition.Add(declaration.Identifier, function, errors);
    }

    private static void Define(Definition definition, Datatype.Declaration declaration, List<Error> errors)
    {
        Define(definition, declaration.Definition, errors);

        Datatype datatype = new()
        {
            Modifiers = declaration.Modifiers,
            Algebra = new Algebra.Unresolved { Reference = declaration.Algebra },
            Definition = declaration.Definition
        };

        definition.Add(declaration.Identifier, datatype, errors);
    }

    private static void Define(Definition definition, Datum.Declaration declaration, List<Error> errors) 
    {
        Datum datum = new()
        {
            Mutability = declaration.Mutability,
            Modifiers = declaration.Modifiers,
            Datatype = new Datatype.Unresolved { Reference = declaration.Datatype },
            Initializer = declaration.Initializer
        };

        definition.Add(declaration.Identifier, datum, errors);
    }
}
