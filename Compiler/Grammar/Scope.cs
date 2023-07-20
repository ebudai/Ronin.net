using Ronin.Compiler;
using Ronin.Grammar.Compound;
using Ronin.Lexicon;

namespace Ronin.Grammar;

internal class Scope : Statement, IParsableSyntax<Scope>
{
    public Modifiers Modifiers { get; init; }
    public Keyword Control { get; init; }
    public Reference Condition { get; init; }    
    public Definition Definition { get; init; }

    public static new Scope Parse(ref Parser current)
    {
        Parser parser = current;

        var modifiers = Modifiers.Parse(ref parser);

        var control = parser.Token as Keyword;
        if (control is not null) parser.Advance();

        if (Reference.Parse(ref parser) is not Reference reference) return null;

        if (Definition.Parse(ref parser) is not Definition definition) return null;

        return new Scope
        {
            Modifiers = modifiers,
            Control = control,
            Condition = reference,
            Definition = definition,
            Source = parser.Commit(ref current)
        };
    }
}
