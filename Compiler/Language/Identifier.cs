using Ronin.Grammar;
using Ronin.Grammar.Compound;
using Ronin.Lexicon;
using System.Text.RegularExpressions;

namespace Ronin.Language;

internal partial class Identifier
{
    public List<Part> Parts { get; init; } = new();

    public Identifier(string name) 
    {
        var names = Whitespace().Split(name);
        var tokens = new Token[names.Length];
        for (var i = 0; i != names.Length; ++i) tokens[i] = new Word(names[i]);
        Words words = new() { Source = tokens };
        Parts.Add(words);
    }

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
                var data = new Parameter[parameters.Values.Count];
                for (int i = 0, max = data.Length; i != max; ++i) data[i] = new Parameter(parameters.Values[i], context);
                Parts.Add(data);
            }
        }
    }

    public class Part
    {
        public Part(Words words) => value = words;
        public Part(Result result) => value = result;
        public Part(Results results) => value = results;
        public Part(Parameter[] data) => value = data;

        public static implicit operator Part(Words words) => new(words);
        public static implicit operator Part(Result result) => new(result);
        public static implicit operator Part(Results results) => new(results);
        public static implicit operator Part(Parameter[] data) => new(data);

        public static implicit operator Words(Part identifier) => identifier.value as Words;
        public static implicit operator Result(Part identifier) => identifier.value as Result;
        public static implicit operator Results(Part identifier) => identifier.value as Results;
        public static implicit operator Parameter[](Part identifier) => identifier.value as Parameter[];

        private readonly object value;
    }

    [GeneratedRegex(@"\w")]
    private static partial Regex Whitespace();
}

internal partial class Error
{
    public static List<Error> IdentifierAlreadyExists(Statement statement) => new() { new IdentifierAlreadyExists { Statement = statement } };
    public static List<Error> AnonymousIdentifier(Statement statement) => new() { new AnonymousIdentifier { Statement = statement } };
    public static List<Error> DataNameCannotIncludeParameters(Statement statement) => new() { new DataNameCannotIncludeParameters { Statement = statement } };
}

internal class IdentifierAlreadyExists : Error { }
internal class AnonymousIdentifier : Error { }
internal class DataNameCannotIncludeParameters : Error { }