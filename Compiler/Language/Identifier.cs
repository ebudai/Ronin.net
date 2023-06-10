using Ronin.Grammar;
using Ronin.Grammar.Compound;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Identifier
{
    public List<Part> Parts { get; init; } = new();

    public Identifier(Name name)
    {
        foreach (var component in name.Components)
        {
            Words words = component;
            if (words is not null) 
            {
                Parts.Add(words);
                continue;
            }

            Parameters parameters = component;            
            if (parameters is not null)
            {
                var data = new Datum[parameters.Values.Count];

            }
        }
    }

    public class Part
    {
        public Part(Words words) => value = words;
        public Part(Datum datum) => value = new[] { datum };
        public Part(Datum[] data) => value = data;
        public Part(Result result) => value = new[] { result };
        public Part(Result[] results) => value = results;

        public static implicit operator Part(Words words) => new(words);
        public static implicit operator Part(Datum[] data) => new(data);
        public static implicit operator Part(Result[] results) => new(results);

        public static implicit operator Words(Part identifier) => identifier.value as Words;
        public static implicit operator Datum[](Part identifier) => identifier.value as Datum[];
        public static implicit operator Result[](Part identifier) => identifier.value as Result[];

        private readonly object value;
    }    
}

[ExcludeFromCodeCoverage]
internal class IdentifierAlreadyExists : Error { }

[ExcludeFromCodeCoverage]
internal class AnonymousIdentifier : Error { }