using Ronin.Compiler;

namespace Ronin.Grammar;

/// <summary>
///     Base class representing any <see cref="AnonymousValue"/> or <see cref="Reference"/>d value
/// </summary>
internal class Value : Statement, IParsableSyntax<Value>
{
    public new static Value Parse(ref Parser current) 
        => AnonymousValue.Parse(ref current) 
        ?? Unresolved.Parse(ref current) as Value;

    public class Unresolved : Value, IParsableSyntax<Unresolved>
    {
        public Reference Reference { get; set; }

        public new static Unresolved Parse(ref Parser current)
        {
            Parser parser = current;
            if (Reference.Parse(ref parser) is not Reference reference) return null;
            return new Unresolved
            {
                Reference = reference,
                Source = parser.Commit(ref current)
            };
        }
    }
}