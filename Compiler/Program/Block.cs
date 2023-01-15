namespace Ronin.Program;

internal class Block
{
    public Block Parent { get; protected private init; }

    public Dictionary<string, Block> Children { get; } = new();
    public Dictionary<string, Block> Imports { get; } = new();
    //public Dictionary<string, Datum> Data { get; } = new();
    public Dictionary<string[], Function> Functions { get; } = new();
    public Dictionary<string[], Datatype> Datatypes { get; } = new();

    public static Block Global { get; }

    static Block()
    {
        Global = new();

        Datatype.CreatePrimitiveDatatypes();

        /*Datum @true = new() { Value = true };
        Datum @false = new() { Value = false };
        Global.Data.Add("true", @true);
        Global.Data.Add("yes", @true);
        Global.Data.Add("false", @false);
        Global.Data.Add("no", @false);

        Datum nothing = new() { Value = null };
        Global.Data.Add("nothing", nothing);*/

        
    }
}
