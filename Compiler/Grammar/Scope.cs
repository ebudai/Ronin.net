using Ronin.Compiler;
using Ronin.Grammar.Compound;
using Ronin.Lexicon.Keywords;

namespace Ronin.Grammar;

internal class Scope : Statement, IParsableSyntax<Scope>
{
    public Modifiers Modifiers { get; init; }
    public Definition Definition { get; init; }

    public static new Scope Parse(ref Parser current)
        => AnonymousScope.Parse(ref current)
        ?? ConditionalScope.Parse(ref current)
        ?? RepeatingScope.Parse(ref current)
        ?? IteratingScope.Parse(ref current) as Scope;
}

internal class AnonymousScope : Scope, IParsableSyntax<AnonymousScope>
{
    public static new AnonymousScope Parse(ref Parser current)
    {
        Parser parser = current;

        var modifiers = Modifiers.Parse(ref parser);

        if (Definition.Parse(ref parser) is not Definition definition) return null;

        return new AnonymousScope
        {
            Modifiers = modifiers,
            Definition = definition,
            Source = parser.Commit(ref current)
        };
    }
}

internal class ConditionalScope : Scope, IParsableSyntax<ConditionalScope>
{
    public Reference Condition { get; init; }

    public static new ConditionalScope Parse(ref Parser current)
    {
        Parser parser = current;

        var modifiers = Modifiers.Parse(ref parser);

        if (parser.TryAdvance<If>() is false) return null;

        if (Reference.Parse(ref parser) is not Reference condition) return null;

        if (Definition.Parse(ref parser) is not Definition definition) return null;

        return new ConditionalScope
        {
            Modifiers = modifiers,
            Condition = condition,
            Definition = definition,
            Source = parser.Commit(ref current)
        };
    }
}

internal class RepeatingScope : Scope, IParsableSyntax<RepeatingScope>
{
    public Reference Condition { get; init; }

    public static new RepeatingScope Parse(ref Parser current)
    {
        Parser parser = current;

        var modifiers = Modifiers.Parse(ref parser);

        if (parser.TryAdvance<While>() is false) return null;

        if (Reference.Parse(ref parser) is not Reference condition) return null;

        if (Definition.Parse(ref parser) is not Definition definition) return null;

        return new RepeatingScope
        {
            Modifiers = modifiers,
            Condition = condition,
            Definition = definition,
            Source = parser.Commit(ref current)
        };
    }
}

internal class IteratingScope : Scope, IParsableSyntax<IteratingScope>
{
    public Datum.Declaration Iterator { get; init; }

    public static new IteratingScope Parse(ref Parser current)
    {
        Parser parser = current;

        var modifiers = Modifiers.Parse(ref parser);

        if (parser.TryAdvance<ForEach>() is false) return null;

        if (Datum.Declaration.Parse(ref parser) is not Datum.Declaration datum) return null;

        if (Definition.Parse(ref parser) is not Definition definition) return null;

        return new IteratingScope
        {
            Modifiers = modifiers,
            Iterator = datum,
            Definition = definition,
            Source = parser.Commit(ref current)
        };
    }
}