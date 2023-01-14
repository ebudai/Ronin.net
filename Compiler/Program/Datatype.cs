namespace Ronin.Program;

internal class Datatype : Block
{
    internal static void CreatePrimitiveDatatypes() => primitives.ForEach(CreatePrimitiveDatatype);

    private static void CreatePrimitiveDatatype(string name) => Global.Datatypes.Add(name.Split(' '), new Datatype { Parent = Global });

    private static readonly List<string> primitives = new()
    {
        "something",
        "nothing",
        "anything",

        "character",
        "text",

        "int8",
        "int16",
        "integer",
        "int64",
        "int128",
        "big integer",

        "small number",
        "number",
        "precise number",
        "rational",

        "money",

        "maybe",

        "byte",
        "bits16",
        "bits32",
        "bits64",
        "bits128",
        "bitset",

        "date",
        "time",
        "datetime"
    };
}
