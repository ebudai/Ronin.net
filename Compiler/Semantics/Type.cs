using Ronin.Grammar;

namespace Ronin.Semantics;

internal partial class Analyzer
{
    public void Types(IContext context = null)
    {
        context ??= Global;

        foreach (var statement in context)
        {
            switch (statement)
            {
                case Datum datum:               Types(datum, context);          break;
                case Function function:         Types(function, context);       break;
                case Association association:   Types(association, context);    break;
            }
        }
    }

    private static void Types(Datum datum, IContext context)
    {
        if (datum is null) return;

        Types(datum.Identifier, context);
        if (datum.Type is Type.Unresolved unresolved)
        {
            var resolution = context.Resolve(unresolved.Reference);
            if (resolution is Resolution.Definite definite)
            {
                datum.Type = definite.Member as Type ?? new Type.Calculated { Member = definite.Member };
            }
            else if (resolution is Resolution.Ambiguous ambiguous)
            {
                datum.Type = new Type.Overloaded { Candidates = ambiguous.Candidates };
            }
        }
    }

    private static void Types(Function function, IContext context)
    {
        Types(function.Identifier, context);
        if (function.Returns is Type.Unresolved unresolved)
        {
            var resolution = context.Resolve(unresolved.Reference);
            if (resolution is Resolution.Definite definite)
            {
                function.Returns = definite.Member as Type ?? new Type.Calculated { Member = definite.Member };
            }
            else if (resolution is Resolution.Ambiguous ambiguous)
            {
                function.Returns = new Type.Overloaded { Candidates = ambiguous.Candidates };
            }
        }
    }

    private static void Types(Association association, IContext context)
    {
        Types(association.Destination, context);
        Types(association.Origin, context);
    }

    private static void Types(Value value, IContext context)
    {
        switch (value)
        {
            case Delegate @delegate:    Types(@delegate, context);  break;
            case Lookup lookup:         Types(lookup, context);     break;
            case Inputs inputs:         Types(inputs, context);     break;
            case List list:             Types(list, context);       break;
            case Index index:           Types(index, context);      break;
        }
    }

    private static void Types(Delegate @delegate, IContext context)
    {
        foreach (var parameter in @delegate.Data)
        {
            Types(parameter.AsDatum, context);
        }
        @delegate.Definition.ResolveTypes(context);
    }

    public static void Types(Lookup lookup, IContext context)
    {
        foreach (var association in lookup)
        {
            Types(association, context);
        }
    }

    public static void Types(Inputs inputs, IContext context)
    {
        foreach (var input in inputs)
        {
            if (input.AsValue is Value value)
            {
                Types(value, context);
            }
            else if (input.AsAssociation is Association association)
            {
                Types(association, context);
            }
        }
    }

    public static void Types(List list, IContext context)
    {
        foreach (var value in list)
        {
            Types(value, context);
        }   
    }

    public static void Types(Index index, IContext context)
    {
        foreach (var value in index)
        {
            Types(value, context);
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
            if (parameter.AsDatum is Datum datum)
            {
                Types(datum, context);
            }
            else if (parameter.AsAssociation is Association association)
            {
                Types(association, context);
            }
        }
    }
}
