using Ronin.Grammar;
using System.Collections.Generic;

using static Ronin.Compiler.Resolution;
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
            case Delegate.Declaration @delegate: DefineDelegate(parent, @delegate); break;
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

    private Datum DefineDatum(Context parent, Datum.Declaration declaration) 
    {
        Datum datum = new()
        {
            Mutability = declaration.Mutability,
            Modifiers = declaration.Modifiers,
            Datatype = new Datatype.Unresolved { Reference = declaration.Datatype },
            Initializer = declaration.Initializer
        };

        if (parent.Add(declaration.Identifier, datum) is Error error) Errors.Add(error);
        
        return datum;
    }

    private Delegate DefineDelegate(Context parent, Delegate.Declaration declaration)
    {
        declaration.Definition.Parent = parent;
        Define(declaration.Definition);

        //TODO: captured vars should be added as data

        List<Datum> data = new(declaration.Parameters.Count);
        foreach (var datum in declaration.Parameters)
        {
            data.Add(DefineDatum(parent, datum));
        }

        Delegate @delegate = new()
        {
            Data = data,
            Definition = declaration.Definition,
            Source = declaration.Source
        };

        return @delegate;
    }
    #endregion

    #region Resolution
    public void Resolve(Module module = null)
    {
        module ??= Global;

        if (Resolved.Add(module) is false) return;

        foreach (var context in module.Contexts) ResolveContext(context);
        foreach (var child in module.Modules.Values) Resolve(child);
    }

    private void ResolveContext(Context context)
    {
        for (int i = 0, max = context.Imports.Count; i != max; ++i)
        {
            
        }

        foreach (var identifier in context.Members.Keys) ResolveParameters(identifier, context);
        context.Members.OnDeserialization(null);

        Dictionary<Identifier.Component, Context.Member> members = new();
        foreach (var (name, member) in context.Members)
        {
            members.Add(name, member switch
            {
                Datatype datatype => ResolveDatatype(datatype, context),
                Function function => ResolveFunction(function, context),
                Datum datum => ResolveDatum(datum, context),
                _ => member
            });
        }
        context.Members = members;

        foreach (var statement in context)
        {
            switch (statement)
            {
                case Comparison assignment: ResolveAssignment(assignment, context); break;
                case Context.Member member: ResolveMember(member, context); break;
                case Value.Anonymous value: ResolveAnonymousValue(value, context); break;
                default: continue;
            }
        }
    }

    private void ResolveImport(Context context, int index)
    {
        if (context.Imports[index] is not Module.Unresolved unresolved) return;
        context.Imports[index] = Global.Resolve(unresolved.Import.Name);
    }

    private void ResolveParameters(Identifier.Component component, Context context)
    {
        if (component.value is not Parameters parameters) return;
        foreach (var datum in parameters.Data.Values)
        {
            datum.Datatype = ResolveDatatype(datum.Datatype, context);
        }
    }

    private Datatype ResolveDatatype(Datatype datatype, Context context)
    {
        if (datatype is not Datatype.Unresolved unresolved) return datatype;

        var resolution = context.Resolve(unresolved.Reference);
        ResolveAlgebra(datatype, context);

        if (resolution is Ambiguous ambiguous)
        {
            return new Datatype.Overloaded
            {
                Algebra = datatype.Algebra,
                Definition = unresolved.Definition,
                Modifiers = unresolved.Modifiers,
                Source = unresolved.Source,
                Overloads = ambiguous.Candidates
            };
        }

        return (resolution as Exact).Member switch
        {
            Datatype resolved => resolved,
            Function function => new Datatype.Calculated<Function> { Member = function },
            Datum datum => new Datatype.Calculated<Datum> { Member = datum },
            _ => ResolutionFailure<Datatype>(unresolved.Reference)
        };
    }

    private Function ResolveFunction(Function function, Context context)
    {
        if (function is not Function.Unresolved unresolved) return function;

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

        return (resolution as Exact).Member switch
        {
            Function resolved => resolved,
            Datum datum => new Function.Calculated { Member = datum },
            _ => ResolutionFailure<Function>(unresolved.Reference)
        };
    }

    private Datum ResolveDatum(Datum datum, Context context)
    {
        datum.Datatype = ResolveDatatype(datum.Datatype, context);

        if (datum is not Datum.Unresolved unresolved) return datum;

        var resolution = context.Resolve(unresolved.Reference);

        if (resolution is Exact and { Member: Datum resolved }) return resolved;
        
        return ResolutionFailure<Datum>(unresolved.Reference);
    }

    private void ResolveAssignment(Comparison assignment, Context context)
    {
        if (assignment.Left is Datum.Unresolved datum)
        {
            assignment.Left = ResolveDatum(datum, context);
        }

        if (assignment.Right is Context.Member.Unresolved member)
        {
            assignment.Right = ResolveMember(member, context);
        }        
    }

    private Context.Member ResolveMember(Context.Member member, Context context)
    {
        if (member is not Context.Member.Unresolved unresolved) return member;

        var resolution = context.Resolve(unresolved.Reference);

        if (resolution is Ambiguous ambiguous)
        {
            return new Context.Member.Overloaded
            {
                Source = unresolved.Reference.Source,
                Overloads = ambiguous.Candidates
            };
        }

        return (resolution as Exact).Member ?? ResolutionFailure<Context.Member>(unresolved.Reference);
    }

    private void ResolveAnonymousValue(Value.Anonymous value, Context context)
    {
        switch (value)
        {
            case Delegate @delegate:  ResolveInputs(@delegate, context); break;
            case Lookup lookup: ResolveLookup(lookup, context); break;
            case Inputs inputs: ResolveInputs(inputs, context); break;
            case List list: ResolveList(list, context); break;
            case Indexer indexer: ResolveIndexer(indexer, context); break;
            default: break;
        };
    }

    private void ResolveAlgebra(Datatype datatype, Context context)
    {
        if (datatype.Algebra is not Algebra.Unresolved unresolved) return;

        var resolution = context.Resolve(unresolved.Reference);

        if (resolution is Ambiguous ambiguous)
        {
            datatype.Algebra = new Algebra.Overloaded
            {
                Source = unresolved.Source,
                Overloads = ambiguous.Candidates
            };
        }
        else
        {
            var exact = resolution as Exact;
            datatype.Algebra = exact.Member switch
            {
                Datatype existing => existing.Algebra,
                Function function => new Algebra.Calculated<Function> { Member = function, Source = exact.Member.Source },                
                Datum datum => new Algebra.Calculated<Datum> { Member = datum, Source = exact.Member.Source },
                _ => ResolutionFailure<Algebra>(unresolved.Reference)
            };
        }
    }

    private void ResolveInputs(Delegate @delegate, Context context)
    {
        for (int i = 0, max = @delegate.Data.Count; i < max; ++i)
        {
            @delegate.Data[i] = ResolveDatum(@delegate.Data[i], context);            
        }
    }

    private void ResolveInputs(Inputs inputs, Context context)
    {
        for (int i = 0, max = inputs.Count; i < max; ++i)
        {
            Value value = inputs[i];
            ResolveValue(value, context);

            Comparison assignment = inputs[i];
            ResolveAssignment(assignment, context);
        }
    }

    private void ResolveLookup(Lookup lookup, Context context)
    {
        foreach (var association in lookup)
        {
            switch (association.Key)
            {
                case Context.Member.Unresolved member: ResolveMember(member, context); break;
                case Value.Anonymous value: ResolveAnonymousValue(value, context); break;
                default: break;
            };

            switch (association.Value)
            {
                case Context.Member.Unresolved member: ResolveMember(member, context); break;
                case Value.Anonymous value: ResolveAnonymousValue(value, context); break;
                default: break;
            };
        }
    }

    private void ResolveValue(Value value, Context context)
    {
        switch (value)
        {
            case Context.Member.Unresolved member: ResolveMember(member, context); break;
            case Value.Anonymous anonymous: ResolveAnonymousValue(anonymous, context); break;
            default: break;
        }
    }

    private void ResolveList(List list, Context context)
    {
        foreach (var value in list)
        {
            ResolveValue(value, context);
        }
    }

    private void ResolveIndexer(Indexer indexer, Context context)
    {
        foreach (var value in indexer)
        {
            ResolveValue(value, context);
        }
    }

    private T ResolutionFailure<T>(Reference reference)
    {
        Errors.Add(Error.UnresolvedReference(reference));
        return default;
    }
    #endregion
}
