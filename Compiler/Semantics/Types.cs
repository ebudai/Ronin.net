using Ronin.Grammar;
using static Ronin.Grammar.Resolution;

namespace Ronin.Semantics;

internal partial class Analyzer
{
    public void Types(IContext context)
    {
        if (context is Scope scope)
        {
            ScopeTypes(scope);
        }
        else if (context is Module module)
        {
            ModuleTypes(module);
        }
    }    

    private void ModuleTypes(Module module)
    {
        foreach (var scope in module.Scopes) 
        {
            ScopeTypes(scope);
        }

        foreach (var submodule in module.Modules.Values)
        {
            ModuleTypes(submodule);
        }
    }

    private void ScopeTypes(Scope scope)
    {
        for (var i = 0; i != scope.Statements.Count; ++i)
        {
            switch (scope.Statements[i])
            {
                case Association association:   AssociationTypes(association, scope);           break;
                case Value value:               scope.Statements[i] = ValueTypes(value, scope); break;
                case Scope subscope:            ScopeTypes(subscope);                           break;
            }
        }
    }

    private void IdentifierTypes(Identifier identifier, IContext context)
    {
        foreach (var component in identifier)
        {
            if (component.AsParameters is not Parameters parameters) continue;
            foreach (var parameter in parameters)
            {
                ParameterTypes(parameter, context);
            }
        }
    }

    private void ParameterTypes(Parameters.Parameter parameter, IContext context)
    {
        if (parameter.AsDatum is Datum datum)
        {
            DatumTypes(datum, context);
        }
        else if (parameter.AsAssociation is Association association)
        {
            AssociationTypes(association, context);
        }
    }

    private void AssociationTypes(Association association, IContext context)
    {
        association.Origin = ValueTypes(association.Origin, context);
        association.Destination = ValueTypes(association.Destination, context);
    }

    private void DatumTypes(Datum datum, IContext context)
    {
        datum.Initializer = ValueTypes(datum.Initializer, context);
        if (datum.Type is Type.Unresolved type)
        {
            IdentifierTypes(datum.Identifier, context);
            datum.Type = type.Resolve(context);            
        }
    }

    private Member MemberTypes(Member member, IContext context)
    {
        switch (member)
        {
            case Function function: FunctionTypes(function, context); break;
            case Datum datum: DatumTypes(datum, context); break;
            case Type.Unresolved type: return type.Resolve(context);
        }
        return member;
    }

    private void FunctionTypes(Function function, IContext context)
    {
        if (function.Returns is Type.Unresolved unresolved)
        {
            function.Returns = unresolved.Resolve(context);
        }

        IdentifierTypes(function.Identifier, context);

        ScopeTypes(function.Definition);
    }

    private void DelegateTypes(Delegate @delegate, IContext context)
    {
        DelegateParameterTypes(@delegate.Data, context);
        ScopeTypes(@delegate.Definition);
    }

    private void DelegateParameterTypes(Delegate.Parameters parameters, IContext context)
    {
        foreach (var parameter in parameters)
        {
            if (parameter.AsDatum is not Datum datum) continue;
            DatumTypes(datum, context);
        }
    }

    private void LookupTypes(Lookup lookup, IContext context)
    {
        foreach (var association in lookup)
        {
            AssociationTypes(association, context);
        }
    }

    private void InputsTypes(Inputs inputs, IContext context)
    {
        for (var i = 0; i != inputs.Count; ++i)
        {
            inputs[i] = InputTypes(inputs[i], context);
        }
    }

    private Inputs.Input InputTypes(Inputs.Input input, IContext context)
    {
        if (input.AsValue is Value value)
        {
            return ValueTypes(value, context);
        }
        
        if (input.AsAssociation is Association association)
        {
            AssociationTypes(association, context);
        }

        return input;
    }

    private void ListTypes(List list, IContext context)
    {
        for (var i = 0; i != list.Count; ++i)
        {
            list[i] = ValueTypes(list[i], context);
        }
    }

    private void IndexTypes(Index index, IContext context)
    {
        for (var i = 0; i != index.Count; ++i)
        {
            index[i] = ValueTypes(index[i], context);
        }
    }

    private Value ValueTypes(Value value, IContext context)
    {
        switch (value)
        {
            case Member resolution:     return MemberTypes(resolution, context);
            case Delegate @delegate:    DelegateTypes(@delegate, context);      break;
            case Lookup lookup:         LookupTypes(lookup, context);           break;
            case Inputs inputs:         InputsTypes(inputs, context);           break;
            case List list:             ListTypes(list, context);               break;
            case Index index:           IndexTypes(index, context);             break;
        }
        return value;
    }
}