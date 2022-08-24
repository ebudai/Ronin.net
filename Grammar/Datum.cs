namespace Ronin.Grammar;

public class Datum : Syntax, IIdentifiable
{
    public Identifier Name { get; set; }
    public Datatype Datatype { get; set; }
    public Modifier Modifiers { get; set; }

    public bool IsMutable => Modifiers.HasFlag(Modifier.Variable) && !Modifiers.HasFlag(Modifier.Readonly);

    [Flags] public enum Modifier
    {
        Variable    = 1 << 0,
        Readonly    = 1 << 1,
        Optional    = 1 << 2,
        Safe        = 1 << 3,
        Public      = 1 << 4,
        Reactive    = 1 << 5,
        Persistent  = 1 << 6,
        Constant    = 1 << 7,
        Shared      = 1 << 8,
    }
}