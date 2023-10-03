using Ronin.Compiler;
using Ronin.Lexicon;
using System.Collections.Generic;

namespace Ronin.Grammar;

internal class Scope : Statement, IParsableSyntax<Scope>
{
    public Modifiers Modifiers { get; init; } = new();
    public Context Definition { get; set; }

    public static new Scope Parse(ref Parser current)
        => AnonymousScope.Parse(ref current)
        ?? ConditionalScope.Parse(ref current)
        ?? RepeatingScope.Parse(ref current)
        ?? IteratingScope.Parse(ref current) as Scope;

    public Identifier Define(Context context, List<Error> errors)
    {
        Definition.Parent = context;
        Identifier name = null;

        foreach (var statement in Definition)
        {
            switch (statement)
            {
                case Export export: export.Define(this, ref name, errors); break;
                case Import import: context.Add(import); break;
                case Function.Declaration function: function.Define(Definition, errors); break;
                case Datatype.Declaration datatype: datatype.Define(Definition, errors); break;
                case Datum.Declaration datum: datum.Define(Definition, errors); break;
                case Delegate.Declaration @delegate: @delegate.Define(Definition, errors); break;
                case Scope scope: scope.Define(Definition, errors); break;
                default: Error.UnknownSyntax(this); break;
            };
        }

        return name;
    }
}

internal class AnonymousScope : Scope, IParsableSyntax<AnonymousScope>
{
    public static new AnonymousScope Parse(ref Parser current)
    {
        Parser parser = current;

        var modifiers = Modifiers.Parse(ref parser);

        if (Context.Parse(ref parser) is not Context definition) return null;

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
    public Condition Condition { get; init; }

    public static new ConditionalScope Parse(ref Parser current)
    {
        Parser parser = current;

        var modifiers = Modifiers.Parse(ref parser);

        if (parser.TryParse<If>() is null) return null;

        if (Condition.Parse(ref parser) is not Condition condition) return null;

        if (Context.Parse(ref parser) is not Context definition) return null;

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
    public Condition Condition { get; init; }

    public static new RepeatingScope Parse(ref Parser current)
    {
        Parser parser = current;

        var modifiers = Modifiers.Parse(ref parser);

        if (parser.TryParse<While>() is null) return null;

        if (Condition.Parse(ref parser) is not Condition condition) return null;

        if (Context.Parse(ref parser) is not Context definition) return null;

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

        if (parser.TryParse<ForEach>() is null) return null;

        var datum = Datum.Declaration.Parse(ref parser);
        var identifier = datum?.Identifier ?? Identifier.Parse(ref parser);

        if (identifier is null) return null;

        if (Context.Parse(ref parser) is not Context definition) return null;

        datum ??= new Datum.Declaration
        {
            Identifier = identifier,
            Source = identifier.Source
        };

        return new IteratingScope
        {
            Modifiers = modifiers,
            Iterator = datum,
            Definition = definition,
            Source = parser.Commit(ref current)
        };
    }
}