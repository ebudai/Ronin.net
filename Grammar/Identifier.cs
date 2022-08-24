using System.Text.RegularExpressions;

namespace Ronin.Grammar;

public class Identifier : Syntax
{
    public Dictionary<int, string> Names { get; } = new();
    public Dictionary<int, Parameters> Parameters { get; } = new();
    public int ComponentCount { get; private set; }

    public Identifier() { }

    public Identifier(string name) => Add(name);

    public void Add(string name)
    {
        name = formatter.Replace(name, " ");
        if (Names.ContainsKey(ComponentCount - 1))
        {
            Names[ComponentCount - 1] += " " + name;
        }
        else
        {
            Names.Add(ComponentCount++, name);
        }
    }

    public void Add(Parameters parameters) => Parameters.Add(ComponentCount++, parameters);

    // this answers if |identifier| contains |this|, and if so, how
    public List<Identifier> Match(Identifier identifier)
    {
        SortedDictionary<int, string> matches = new();
        
        foreach (var name in Names)
        {
            if (identifier.Names.ContainsValue(name.Value))
            {
                matches.Add(name.Key, name.Value);
            }
        }

        // if the order is wrong but all names matched, this will still be false
        if (Enumerable.SequenceEqual(matches, Names))
        {
            List<Identifier> identifiers = new();
            var (left, right) = identifier.Split(matches.Keys.First());
            identifiers.Add(left);
            foreach (var index in matches.Keys.Skip(1))
            {
                (left, right) = right.Split(index);
                identifiers.Add(left);
            }
            return identifiers;
        }

        return null;
    }

    private (Identifier left, Identifier right) Split(int index)
    {
        Identifier left = new();
        Identifier right = new();

        foreach (var name in Names)
        {
            if (name.Key < index) left.Names.Add(name.Key, name.Value);
            else if (name.Key > index) right.Names.Add(name.Key, name.Value);
        }
        foreach (var parameter in Parameters)
        {
            if (parameter.Key < index) left.Parameters.Add(parameter.Key, parameter.Value);
            else if (parameter.Key > index) right.Parameters.Add(parameter.Key, parameter.Value);
        }

        return (left, right);
    }

    private static readonly Regex formatter = new(@"\s+", RegexOptions.Multiline);
}

public interface IIdentifiable
{
    public Identifier Name { get; }
}