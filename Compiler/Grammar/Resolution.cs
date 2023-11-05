using Ronin.Compiler;
using System.Collections.Generic;

namespace Ronin.Grammar;

internal abstract class Resolution : Value
{
    public static Resolution From(List<Resolution> resolutions)
    {
        return resolutions.Count switch
        {
            0 => null,
            1 => resolutions[0],
            _ => new Ambiguous { Candidates = resolutions }
        };
    }

    public class Definite : Resolution
    {
        public Member Member { get; set; }
        public List<Resolution> Inputs { get; init; } = new();
    }

    public class Ambiguous : Resolution
    {
        public List<Resolution> Candidates { get; init; } = new();
    }
}
