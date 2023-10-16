using Ronin.Compiler;

namespace Ronin.Grammar;

internal class Resolution : Value, IParsable<Resolution>
{
    public static new Resolution Parse(ref Parser current)
    {
        Parser parser = current;

        if (Reference.Parse(ref parser) is not Reference reference) return null;

        foreach (var component in reference)
        {
            if (component.AsName is not null)
            {
                current = parser;
                return new Unresolved { Reference = reference };
            }
        }

        return null;
    }

    public class Unresolved : Resolution
    {
        public Reference Reference { get; init; }
    }
}
