using Ronin.Grammar;
using System.Collections.Generic;

namespace Ronin.Hierarchy;

internal class Resolution
{
    public Context.Member Member { get; set; }
    public Possibilities[] Inputs { get; set; } 

    public int Size => 1 + Inputs.Length;
}

internal class Possibilities : List<Resolution>
{
    
}