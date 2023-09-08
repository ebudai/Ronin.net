using Ronin.Grammar;
using Ronin.Hierarchy;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Datatype = Ronin.Grammar.Datatype;
using Function = Ronin.Grammar.Function;
using Import = Ronin.Grammar.Import;

namespace Ronin.Compiler;

internal class Analyzer
{
    public Module Global { get; } = new();
    public List<Error> Errors { get; } = new();
    private readonly HashSet<Module> Resolved = new(ReferenceEqualityComparer.Instance);

    #region Definition
    public void Define(Context context)
    {
        foreach (var statement in context)
        {
            if (statement is Export export)
            {
                Errors.Add(Error.ScopeMustBeAnonymous(context, export));
                continue;
            }
            DefineStatement(context, statement);
        }
    }

    public void DefineScope(Context parent, Scope scope)
    {
        scope.Definition.Parent = parent;
        Identifier name = null;
        
        foreach (var statement in scope.Definition)
        {
            if (statement is Export export)
            {
                Export(scope, export, ref name);
                continue;
            }
            DefineStatement(scope.Definition, statement);
        }
        
        if (ReferenceEquals(parent, Global) || name is not null)
        {
            Global.Add(scope.Definition, name);
        }
    }

    private void DefineStatement(Context parent, Statement statement)
    {
        switch (statement)
        {
            case Import import: parent.Add(import); break;
            case Function.Declaration function: DefineFunction(parent, function); break;
            case Datatype.Declaration datatype: DefineDatatype(parent, datatype); break;
            case Datum.Declaration datum: DefineDatum(parent, datum); break;
            case Scope inner: DefineScope(parent, inner); break;
            case Unknown unknown: Errors.Add(Error.UnknownSyntax(unknown)); break;
            default: break;
        }
    }

    private void Export(Scope scope, Export export, ref Identifier identifier)
    {
        bool error = false;

        if (scope is not AnonymousScope)
        {
            Errors.Add(Error.ScopeMustBeAnonymous(scope.Definition, export));
            error = true;
        }

        if (scope.Modifiers.Source.IsEmpty is false)
        {
            Errors.Add(Error.ScopeMustBeUnmodified(scope.Definition, export));
            error = true;
        }

        if (identifier is not null)
        {
            Errors.Add(Error.ScopeIsAlreadyPartOfAModule(scope.Definition, export));
            error = true;
        }
        
        if (error is false) identifier = export.Identifier;
    }

    private void DefineFunction(Context parent, Function.Declaration declaration)
    {
        declaration.Definition.Parent = parent;
        Define(declaration.Definition);
        
        Function function = new()
        {
            Modifiers = declaration.Modifiers,
            Returns = new Datatype.Unresolved { Reference = declaration.Returns },
            Definition = declaration.Definition,
        };

        if (parent.Add(declaration.Identifier, function) is Error error) Errors.Add(error);
    }

    private void DefineDatatype(Context parent, Datatype.Declaration declaration)
    {
        declaration.Definition.Parent = parent;
        Define(declaration.Definition);

        Datatype datatype = new()
        {
            Modifiers = declaration.Modifiers,
            Algebra = new Algebra.Unresolved { Reference = declaration.Algebra },
            Definition = declaration.Definition
        };

        if (parent.Add(declaration.Identifier, datatype) is Error error) Errors.Add(error);
    }

    private void DefineDatum(Context parent, Datum.Declaration declaration) 
    {
        Datum datum = new()
        {
            Mutability = declaration.Mutability,
            Modifiers = declaration.Modifiers,
            Datatype = new Datatype.Unresolved { Reference = declaration.Datatype },
            Initializer = declaration.Initializer
        };

        if (parent.Add(declaration.Identifier, datum) is Error error) Errors.Add(error);
    }
    #endregion

    #region Resolution
    public void Resolve(Module module)
    {
        if (Resolved.Add(module) is false) return;

        foreach (var context in module.Contexts) ResolveContext(context);

        foreach (var child in module.Modules.Values) Resolve(child);
    }

    private void ResolveContext(Context context)
    {
        for (int i = 0, max = context.Imports.Count; i != max; ++i)
        {
            if (context.Imports[i] is not Module.Unresolved unresolved) continue;
            context.Imports[i] = Global.GetOrCreate(unresolved.Import.Name);            
        }

        foreach (var identifier in context.Members.Keys) ResolveIdentifierParameters(identifier, context);
        context.Members.OnDeserialization(null);

        foreach (var (name, member) in context.Members)
        {
            context.Members[name] = member switch
            {
                Datatype.Unresolved unresolved => ResolveDatatype(unresolved, context),
                Function.Unresolved unresolved => ResolveFunction(unresolved, context),
                Datum.Unresolved unresolved => ResolveDatum(unresolved, context),
                _ => member
            };
        }

        foreach (var statement in context)
        {
            switch (statement)
            {
                case Assignment assignment: ResolveAssignment(assignment, context); break;
                case Context.Member.Unresolved reference: ResolveReference(reference, context); break;
                case Value.Anonymous value: ResolveValue(value, context); break;
                default: continue;
            }
        }
    }

    private void ResolveIdentifierParameters(Identifier identifier, Context context)
    {
        foreach (var component in identifier.Components) ResolveParameters(component, context);
    }

    private void ResolveParameters(Identifier.Component component, Context context)
    {
        if (component.value is not Parameters parameters) return;
        foreach (var datum in parameters.Data.Values)
        {
            if (datum.Datatype is not Datatype.Unresolved datatype) continue;
            datum.Datatype = ResolveDatatype(datatype, context);
        }
    }

    private Datatype ResolveDatatype(Datatype.Unresolved unresolved, Context context)
    {
        var resolution = context.Resolve(unresolved.Reference);
        var algebra = ResolveAlgebra(unresolved.Algebra as Algebra.Unresolved, context);

        if (resolution is Ambiguous ambiguous)
        {
            return new Datatype.Overloaded
            {
                Algebra = algebra,
                Definition = unresolved.Definition,
                Modifiers = unresolved.Modifiers,
                Source = unresolved.Source,
                Overloads = ambiguous.Candidates
            };
        }

        return resolution.Member switch
        {
            Datatype datatype => datatype,
            Function function => new Datatype.Calculated<Function> { Member = function },
            Datum datum => new Datatype.Calculated<Datum> { Member = datum },
            _ => ResolutionFailure<Datatype>(unresolved.Reference)
        };
    }

    private Function ResolveFunction(Function.Unresolved unresolved, Context context)
    {
        var resolution = context.Resolve(unresolved.Reference);
        var returns = ResolveDatatype(unresolved.Returns as Datatype.Unresolved, context);

        if (resolution is Ambiguous ambiguous)
        {
            return new Function.Overloaded
            {
                Definition = unresolved.Definition,
                Modifiers = unresolved.Modifiers,
                Returns = returns,
                Source = unresolved.Source,
                Overloads = ambiguous.Candidates
            };
        }

        return resolution.Member switch
        {
            Function function => function,
            Datum datum => new Function.Calculated { Member = datum },
            _ => ResolutionFailure<Function>(unresolved.Reference)
        };
    }

    private Datum ResolveDatum(Datum.Unresolved unresolved, Context context)
    {
        var resolution = context.Resolve(unresolved.Reference);

        if (resolution is not Ambiguous and { Member: Datum datum }) return datum;
        
        return ResolutionFailure<Datum>(unresolved.Reference);
    }

    private void ResolveAssignment(Assignment assignment, Context context)
    {
        if (assignment.Destination is Datum.Unresolved datum) ResolveDatum(datum, context);

    }

    private Reference ResolveReference(Context.Member.Unresolved reference, Context context)
    {
        return null;
    }

    private void ResolveValue(Value.Anonymous value, Context context)
    {

    }

    private Algebra ResolveAlgebra(Algebra.Unresolved unresolved, Context context)
    {
        var resolution = context.Resolve(unresolved.Reference);

        if (resolution is Ambiguous ambiguous)
        {
            return new Algebra.Overloaded
            {
                Source = unresolved.Source,
                Overloads = ambiguous.Candidates
            };
        }

        return resolution.Member switch
        {
            Function function => new Algebra.Calculated<Function> { Member = function },
            Datatype datatype => new Algebra.Calculated<Datatype> { Member = datatype },
            Datum datum => new Algebra.Calculated<Datum> { Member = datum },
            _ => ResolutionFailure<Algebra>(unresolved.Reference)
        };
    }

    private T ResolutionFailure<T>(Reference reference)
    {
        Errors.Add(Error.UnresolvedReference(reference));
        return default;
    }
    #endregion
}
