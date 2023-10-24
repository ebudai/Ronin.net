using Ronin.Compiler;
using System.Collections.Generic;

namespace Ronin.Grammar;

internal abstract class Resolution : Value, IParsable<Resolution>
{
    public static Resolution From(List<Resolution> resolutions)
    {
        return resolutions.Count switch
        {
            0 => null,
            1 => resolutions[0],
            _ => new Resolution.Ambiguous { Candidates = resolutions }
        };
    }

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

    public class Definite : Resolution
    {
        public Member Member { get; init; }
        public List<Resolution> Inputs { get; init; } = new();
    }

    public class Ambiguous : Resolution
    {
        public List<Resolution> Candidates { get; init; } = new();
    }
}
