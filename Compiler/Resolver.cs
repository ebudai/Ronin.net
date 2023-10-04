using Ronin.Grammar;
using System;
using System.Collections.Generic;

namespace Ronin.Compiler;

internal abstract class Resolution
{
    public static Resolution From(List<Resolution> resolutions) => resolutions.Count switch
    {
        0 => null,
        1 => resolutions[0],
        _ => new Ambiguous { Candidates = resolutions }
    };

    public static Resolution Match(Context context, Identifier name, Reference reference)
    {
        throw new NotImplementedException();
    }

    public class Exact : Resolution
    {
        public Context.Member Member { get; set; }
        public List<Resolution> Inputs { get; } = new();
    }

    public class Ambiguous : Resolution
    {
        public List<Resolution> Candidates { get; init; }
    }
}