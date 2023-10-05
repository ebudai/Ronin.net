using Ronin.Grammar;
using System.Collections.Generic;

using static Ronin.Compiler.Resolution;
using Type = Ronin.Grammar.Type;
using Delegate = Ronin.Grammar.Delegate;
using Function = Ronin.Grammar.Function;

namespace Ronin.Compiler;

internal class Analyzer
{
    public Module Global { get; init; } = new();
    public List<Error> Errors { get; } = new();
    private readonly HashSet<Module> Resolved = new(ReferenceEqualityComparer.Instance);

    #region Definition
    public void Define()
    {
        Global.Define(null, Errors);
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
            ResolveImport(context, i);
        }

        foreach (var identifier in context.Members.Keys) ResolveParameters(identifier, context);
        context.Members.OnDeserialization(null);

        Dictionary<Identifier.Component, Context.Member> members = new();
        foreach (var (name, member) in context.Members)
        {
            members.Add(name, member switch
            {
                Type datatype => ResolveDatatype(datatype, context),
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
                //case Comparison assignment: ResolveAssignment(assignment, context); break;
                case Context.Member member: ResolveMember(member, context); break;
                case Value value: ResolveValue(value, context); break;
                default: continue;
            }
        }
    }

    private void ResolveImport(Context context, int index)
    {
        if (context.Imports[index] is not Module.Unresolved unresolved) return;
        context.Imports[index] = Global.Resolve(unresolved.Import.Name);
    }

    private void ResolveParameters(Component component, Context context)
    {
        if (component.value is not Parameters parameters) return;
        foreach (var datum in parameters.Data.Values)
        {
            datum.Datatype = ResolveDatatype(datum.Datatype, context);
        }
    }

    private Type ResolveDatatype(Type datatype, Context context)
    {
        if (datatype is not Type.Unresolved unresolved) return datatype;

        var resolution = context.Resolve(unresolved.Reference);
        ResolveAlgebra(datatype, context);

        if (resolution is Ambiguous ambiguous)
        {
            return new Type.Overloaded
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
            Type resolved => resolved,
            Function function => new Type.Calculated<Function> { Member = function },
            Datum datum => new Type.Calculated<Datum> { Member = datum },
            _ => ResolutionFailure<Type>(unresolved.Reference)
        };
    }

    private Function ResolveFunction(Function function, Context context)
    {
        if (function is not Function.Unresolved unresolved) return function;

        var resolution = context.Resolve(unresolved.Reference);
        var returns = ResolveDatatype(unresolved.Returns as Type.Unresolved, context);

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
        if (assignment.Left.value is Datum.Unresolved datum)
        {
            assignment.Left.value = ResolveDatum(datum, context);
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

    private void ResolveValue(Value value, Context context)
    {
        switch (value)
        {
            case Context.Member.Unresolved member: ResolveMember(member, context); break;
            case Delegate @delegate:  ResolveInputs(@delegate, context); break;
            case Lookup lookup: ResolveLookup(lookup, context); break;
            case Inputs inputs: ResolveInputs(inputs, context); break;
            case List list: ResolveList(list, context); break;
            case Indexer indexer: ResolveIndexer(indexer, context); break;
            default: break;
        }
    }

    private void ResolveAlgebra(Type datatype, Context context)
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
                Type existing => existing.Algebra,
                Function function => new Algebra.Calculated<Function> { Member = function, Source = exact.Member.Source },                
                Datum datum => new Algebra.Calculated<Datum> { Member = datum, Source = exact.Member.Source },
                _ => ResolutionFailure<Algebra>(unresolved.Reference)
            };
        }
    }

    private void ResolveInputs(Delegate @delegate, Context context)
    {
        foreach (var name in @delegate.Data.Keys)
        {
            @delegate.Data[name] = ResolveDatum(@delegate.Data[name], context);
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
                case Value value: ResolveValue(value, context); break;
                default: break;
            }

            switch (association.Value)
            {
                case Context.Member.Unresolved member: ResolveMember(member, context); break;
                case Value value: ResolveValue(value, context); break;
                default: break;
            }
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
