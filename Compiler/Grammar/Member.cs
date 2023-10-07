using Ronin.Compiler;

namespace Ronin.Grammar;

internal class Member : Value, IAggregable<Member>
{
    public static new Member Parse(ref Parser current)
        => Function.Parse(ref current)
        ?? Type.Parse(ref current)
        ?? Datum.Parse(ref current) as Member;
    
    public class Unresolved : Member
    {
        public Reference Reference { get; init; }

        public static new Member Parse(ref Parser parser) 
            => Reference.Parse(ref parser) is not Reference reference 
                ? null
                : new Unresolved { Reference = reference };
    }
}
