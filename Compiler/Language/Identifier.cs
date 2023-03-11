using Ronin.Grammar;

namespace Ronin.Language;

#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
internal class Identifier : Semantics
{
    public List<IComponent> Components { get; init; } = new();

    public Identifier(IdentifierSyntax identifier)
    {
        foreach (var component in identifier.Components)
        {
            Components.Add(component.value switch
            {
                Grammar.Name name => new Name(name),
                Grammar.Aggregates.Parameters parameters => new Parameters(parameters),
            });
        }
    }

    /*public int IndexOf(Reference reference)
    {
        int index = 0;
        foreach (var component in Components)
        {

        }
    }*/

    public interface IComponent
    {

    }

    public class Name : Grammar.Name, IComponent
    {
        public Name(Grammar.Name name) => Source = name.Source;
    }

    public class Parameters : List<Datum>, IComponent
    {
        public Parameters(Grammar.Aggregates.Parameters parameters)
        {
            foreach (var parameter in parameters.Values) Add(new Datum(parameter));
        }
    }
}
