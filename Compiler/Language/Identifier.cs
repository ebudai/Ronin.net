using Ronin.Grammar;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Language;

[ExcludeFromCodeCoverage]
internal class Identifier
{
    public Identifier(Words words) => value = words;
    public Identifier(Datum datum) => value = new[] { datum };
    public Identifier(Datum[] data) => value = data;
    
    public Identifier Stamp(Result result)
    {
        throw new NotImplementedException();
    }

    public Identifier Stamp(Result[] results)
    {
        throw new NotImplementedException();
    }

    public override bool Equals(object obj)
    {
        if (obj is not Identifier identifier) return false;
        if (value is Words words && identifier.value is Words otherwords)
        {
            return MemoryExtensions.SequenceEqual(words.Source.Span, otherwords.Source.Span);
        }
        else if (value is Datum[] data)
        {
            if (identifier.value is Datum[] otherdata)
            {

            }
            else if (identifier.value is Result[] results)
            {

            }
        }
        else if (value is Result[] results)
        {
            if (identifier.value is Result[] otherresults)
            {

            }
        }
        return false;
    }

    public override int GetHashCode()
    {
        if (value is Words words) return words.GetHashCode();
        HashCode hash = new();
        if (value is Datum[] data) foreach (var datum in data) hash.Add(datum);
        else if (value is Result[] results) foreach (var result in results) hash.Add(result);
        return hash.ToHashCode();
    }

    public static implicit operator Words(Identifier identifier) => identifier.value as Words;
    public static implicit operator Datum[](Identifier identifier) => identifier.value as Datum[];
    public static implicit operator Result[](Identifier identifier) => identifier.value as Result[];

    private readonly object value;
}

[ExcludeFromCodeCoverage]
internal class IdentifierAlreadyExists : Error { }