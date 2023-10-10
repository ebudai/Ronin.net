using Ronin.Grammar;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Ronin.Semantics;

internal partial class Resolver
{
    public Resolution Dereference(Scope context, Identifier identifier, Reference reference)
    {
        
        
        return null;
    }

       

    [ExcludeFromCodeCoverage]
    public abstract class Resolution
    {
        public static Resolution From(List<Resolution> resolutions) => resolutions.Count switch
        {
            0 => null,
            1 => resolutions[0],
            _ => new Ambiguous { Candidates = resolutions }
        };

        public static Resolution Match(Scope context, Identifier name, Reference reference)
        {
            throw new NotImplementedException();
        }

        public class Exact : Resolution
        {
            public Member Member { get; set; }
            public List<Resolution> Inputs { get; } = new();
        }

        public class Ambiguous : Resolution
        {
            public List<Resolution> Candidates { get; init; }
        }
    }
}
