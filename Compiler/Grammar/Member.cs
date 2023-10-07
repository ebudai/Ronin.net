using Ronin.Compiler;

namespace Ronin.Grammar;

internal class Member : Value, IParsable<Member>
{
    public static new Member Parse(ref Parser current) => Unresolved.Parse(ref current);
    
    public class Unresolved : Member
    {
        public Reference Reference { get; init; }

        public static new Member Parse(ref Parser parser)
        {
            if (Reference.Parse(ref parser) is not Reference reference) return null;
            return new Unresolved { Reference = reference };
        }
    }
}
