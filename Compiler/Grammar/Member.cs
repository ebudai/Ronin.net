using Ronin.Compiler;

namespace Ronin.Grammar;

internal class Member : Syntax, Compiler.IParsable<Member>
{
    public static Member Parse(ref Parser context)
    {
        Parser parser = context;

        var syntax = Datum.Parse(ref parser) ?? Function.Parse(ref parser) as Syntax;

        if (syntax is null) return null;

        return new Member { value = syntax, Source = parser.Commit(ref context) };
    }

    public static implicit operator Datum(Member member) => member.value as Datum;
    public static implicit operator Function(Member member) => member.value as Function;

    private Syntax value;
}
