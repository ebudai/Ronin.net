using Ronin.Grammar;

namespace Ronin.Program;

internal class Fundamental
{
    public static readonly Grammar.Datatype something = GenerateFundamentalDatatype(nameof(something));
    public static readonly Grammar.Datatype nothing = GenerateFundamentalDatatype(nameof(nothing));
    public static readonly Grammar.Datatype anything = GenerateFundamentalDatatype(nameof(anything));

    public static readonly Grammar.Datatype character = GenerateFundamentalDatatype(nameof(character));
    public static readonly Grammar.Datatype text = GenerateFundamentalDatatype(nameof(text));
    
    public static readonly Grammar.Datatype int8 = GenerateFundamentalDatatype(nameof(int8));
    public static readonly Grammar.Datatype int16 = GenerateFundamentalDatatype(nameof(int16));
    public static readonly Grammar.Datatype integer = GenerateFundamentalDatatype(nameof(integer));
    public static readonly Grammar.Datatype int64 = GenerateFundamentalDatatype(nameof(int64));
    public static readonly Grammar.Datatype int128 = GenerateFundamentalDatatype(nameof(int128));
    public static readonly Grammar.Datatype biginteger = GenerateFundamentalDatatype(nameof(biginteger));

    public static readonly Grammar.Datatype smallnumber = GenerateFundamentalDatatype(nameof(smallnumber));
    public static readonly Grammar.Datatype number = GenerateFundamentalDatatype(nameof(number));
    public static readonly Grammar.Datatype precisenumber = GenerateFundamentalDatatype(nameof(precisenumber));
    public static readonly Grammar.Datatype rational = GenerateFundamentalDatatype(nameof(rational));

    public static readonly Grammar.Datatype money = GenerateFundamentalDatatype(nameof(money));

    public static readonly Grammar.Datatype maybe = GenerateFundamentalDatatype(nameof(maybe));

    public static readonly Grammar.Datatype @byte = GenerateFundamentalDatatype(nameof(@byte));
    public static readonly Grammar.Datatype bytes16 = GenerateFundamentalDatatype(nameof(bytes16));
    public static readonly Grammar.Datatype bytes32 = GenerateFundamentalDatatype(nameof(bytes32));
    public static readonly Grammar.Datatype bytes64 = GenerateFundamentalDatatype(nameof(bytes64));
    public static readonly Grammar.Datatype bytes128 = GenerateFundamentalDatatype(nameof(bytes128));
    public static readonly Grammar.Datatype bitset = GenerateFundamentalDatatype(nameof(bitset));

    public static readonly Grammar.Datatype date = GenerateFundamentalDatatype(nameof(date));
    public static readonly Grammar.Datatype time = GenerateFundamentalDatatype(nameof(time));
    public static readonly Grammar.Datatype datetime = GenerateFundamentalDatatype(nameof(datetime));


    private static Grammar.Datatype GenerateFundamentalDatatype(string name) => new()
    {
        Is = new(),
        Identifier = new() { Components = { new Name { Words = new[] { name } } } },
    };
}
