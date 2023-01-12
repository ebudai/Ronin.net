using Ronin.Grammar;

namespace Ronin.Program;

internal class Primitive
{
    public static readonly Grammar.Datatype.Declaration something = GeneratePrimitive(nameof(something));
    public static readonly Grammar.Datatype.Declaration nothing = GeneratePrimitive(nameof(nothing));
    public static readonly Grammar.Datatype.Declaration anything = GeneratePrimitive(nameof(anything));

    public static readonly Grammar.Datatype.Declaration character = GeneratePrimitive(nameof(character));
    public static readonly Grammar.Datatype.Declaration text = GeneratePrimitive(nameof(text));
    
    public static readonly Grammar.Datatype.Declaration int8 = GeneratePrimitive(nameof(int8));
    public static readonly Grammar.Datatype.Declaration int16 = GeneratePrimitive(nameof(int16));
    public static readonly Grammar.Datatype.Declaration integer = GeneratePrimitive(nameof(integer));
    public static readonly Grammar.Datatype.Declaration int64 = GeneratePrimitive(nameof(int64));
    public static readonly Grammar.Datatype.Declaration int128 = GeneratePrimitive(nameof(int128));
    public static readonly Grammar.Datatype.Declaration biginteger = GeneratePrimitive(nameof(biginteger));

    public static readonly Grammar.Datatype.Declaration smallnumber = GeneratePrimitive(nameof(smallnumber));
    public static readonly Grammar.Datatype.Declaration number = GeneratePrimitive(nameof(number));
    public static readonly Grammar.Datatype.Declaration precisenumber = GeneratePrimitive(nameof(precisenumber));
    public static readonly Grammar.Datatype.Declaration rational = GeneratePrimitive(nameof(rational));

    public static readonly Grammar.Datatype.Declaration money = GeneratePrimitive(nameof(money));

    public static readonly Grammar.Datatype.Declaration maybe = GeneratePrimitive(nameof(maybe));

    public static readonly Grammar.Datatype.Declaration @byte = GeneratePrimitive(nameof(@byte));
    public static readonly Grammar.Datatype.Declaration bytes16 = GeneratePrimitive(nameof(bytes16));
    public static readonly Grammar.Datatype.Declaration bytes32 = GeneratePrimitive(nameof(bytes32));
    public static readonly Grammar.Datatype.Declaration bytes64 = GeneratePrimitive(nameof(bytes64));
    public static readonly Grammar.Datatype.Declaration bytes128 = GeneratePrimitive(nameof(bytes128));
    public static readonly Grammar.Datatype.Declaration bitset = GeneratePrimitive(nameof(bitset));

    public static readonly Grammar.Datatype.Declaration date = GeneratePrimitive(nameof(date));
    public static readonly Grammar.Datatype.Declaration time = GeneratePrimitive(nameof(time));
    public static readonly Grammar.Datatype.Declaration datetime = GeneratePrimitive(nameof(datetime));


    private static Grammar.Datatype.Declaration GeneratePrimitive(string name)
    {
        return new()
        {
            Is = new(),
            Identifier = new() { Components = { new Name { Words = new[] { name } } } },            
        };
    }
}
