using Ronin.Grammar.Modifier;

namespace Ronin.Grammar;

public class Datum : Modifiable
{
    public Identifier Name { get; set; }
    public Datatype Datatype { get; set; }
}