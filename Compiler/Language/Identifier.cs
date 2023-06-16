using Ronin.Grammar;
using Ronin.Grammar.Compound;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Identifier
{
    public List<Part> Parts { get; init; } = new();

    public Identifier(Name name, Context context)
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
                for (int i = 0, max = data.Length; i != max; ++i) data[i] = new Datum(parameters.Values[i], context);
                Parts.Add(data);
            }
        }
    }

    public class Part
    {
        public Part(Words words) => value = words;
        public Part(Result result) => value = result;
        public Part(Results results) => value = results;
        public Part(Datum[] data) => value = data;

        public static implicit operator Part(Words words) => new(words);
        public static implicit operator Part(Result result) => new(result);
        public static implicit operator Part(Results results) => new(results);
        public static implicit operator Part(Datum[] data) => new(data);

        public static implicit operator Words(Part identifier) => identifier.value as Words;
        public static implicit operator Result(Part identifier) => identifier.value as Result;
        public static implicit operator Results(Part identifier) => identifier.value as Results;
        public static implicit operator Datum[](Part identifier) => identifier.value as Datum[];

        private readonly object value;
    }    
}

internal partial class Error
{
    public static List<Error> IdentifierAlreadyExists(Statement statement) => new() { new IdentifierAlreadyExists { Statement = statement } };
    public static List<Error> AnonymousIdentifier(Statement statement) => new() { new AnonymousIdentifier { Statement = statement } };
}

[ExcludeFromCodeCoverage]
internal class IdentifierAlreadyExists : Error { }

[ExcludeFromCodeCoverage]
internal class AnonymousIdentifier : Error { }