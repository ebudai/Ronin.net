namespace Ronin.Grammar;

internal class Datatype : Syntax
{
    internal Identifier Name { get; set; }
    internal List<Function> Parameters { get; } = new(); 
    internal List<Datatype> MemberVariables { get; } = new();
}
