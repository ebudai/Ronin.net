using Ronin.Grammar;
using Ronin.Grammar.Compound;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Identifier
{
    public List<Component> Components { get; init; } = new();

    public Identifier(Name name, Context context)
    {
        foreach (var part in name.Components)
        {
            Words words = part;
            if (words is not null)
            {
                Components.Add(words);
                continue;
            }
            
            Parameters parameters = part;
            var max = parameters.Values.Count;
            var data = new Datum[max];
            for (var i = 0; i != max; ++i) data[i] = new Datum(parameters.Values[i], context);            
        }
    }

    public class Component
    {
        public static implicit operator Component(Words words) => new() { value = words };
        public static implicit operator Component(Datum[] data) => new() { value = data };
        public static implicit operator Component(Result result) => new() { value = result };

        public static implicit operator Words(Component component) => component.value as Words;
        public static implicit operator Datum[](Component component) => component.value as Datum[];
        public static implicit operator Result(Component component) => component.value as Result;

        private object value;
    }
}

[ExcludeFromCodeCoverage]
internal class IdentifierAlreadyExists : Error { }