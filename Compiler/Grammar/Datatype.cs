namespace Ronin.Grammar;

internal class Datatype : Syntax
{
    internal Identifier Name { get; set; }
    internal List<Datatype> MemberVariables { get; set; }
}
