using Ronin.Compiler;

namespace Ronin.Grammar;

internal class Member : Value, IParsable<Member>
{
    public static new Member Parse(ref Parser current) => Unresolved.Parse(ref current);
    
    public class Unresolved : Member
    {
        public Reference Reference { get; init; }

        public static new Member Parse(ref Parser current)
        {
            Parser parser = current;
            if (Reference.Parse(ref parser) is not Reference reference) return null;
            foreach (var component in reference)
            {
                if (component.IsT0)
                {
                    current = parser;
                    return new Unresolved { Reference = reference };
                }
            }
            return null; // members can only have a name
        }
    }
}
