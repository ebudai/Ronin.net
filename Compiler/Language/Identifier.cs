using Ronin.Grammar;
using Ronin.Grammar.Compound;

namespace Ronin.Language;

/*#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
internal class Identifier : Semantics
{
    public List<IComponent> Components { get; init; } = new();

    public Identifier(Grammar.Identifier identifier)
    {
        foreach (var component in identifier.Components)
        {
            Components.Add(component.value switch
            {
                Grammar.Name name => new Name(name),
                Grammar.Compound.Parameters parameters => new Parameters(parameters),
            });
        }
    }

    public int Index(Reference reference)
    {
        var start = 0;
        foreach (var component in Components)
        {
            if (component.Matches(reference.Components[start])) break;
            ++start;
        }
        if (start == Components.Count) return -1;
        if (start + Components.Count > reference.Components.Count) return -1;

        for (int i = start, max = Components.Count; i != max; ++i)
        {
            if (Components[i - start].Matches(reference.Components[i]) is not true) return -1;
        }
        return start;
    }

    public interface IComponent
    {
        public bool Matches(Reference.Component component);
    }

    public class Name : Grammar.Name, IComponent
    {
        public Name(Grammar.Name name) => Source = name.Source;

        public bool Matches(Reference.Component component)
        {
            if (component.value is not Grammar.Name name) return false;
            if (Source.Length != name.Source.Length) return false;
            for (int i = 0, max = Source.Length; i != max; ++i)
            {
                var left = Source.Span[i].sourcecode.Span;
                var right = name.Source.Span[i].sourcecode.Span;
                if (left.SequenceEqual(right) is not true) return false;
            }
            return true;
        }
    }

    public class Parameters : List<Datum>, IComponent
    {
        public Parameters(Grammar.Compound.Parameters parameters)
        {
            foreach (var parameter in parameters.Values) Add(new Datum(parameter));
        }

        public bool Matches(Reference.Component component) => component.value switch
        {
            Name or Literal => Count is 1,
            Arguments arguments => Count == arguments.Values.Count,
        };
    }
}*/
