// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;

using Type = Ronin.Grammar.Type;
using Function = Ronin.Grammar.Function;

namespace Failure;

/// <summary>
///     A consumed <c>=&gt;</c> or <c>=</c> commits: what follows is a type or a
///     value, and nothing following is a mistake.
/// </summary>
///
/// <remarks>
///     <para>
///     All of these compiled with no complaint. Leaving the right-hand side
///     optional after the symbol had already been consumed meant the mistake
///     landed on a form the language really has — «var x =&gt; = 1» became an
///     untyped declaration of «x», «function f =&gt; {}» a function with no
///     return type, «type T = ;» a plain type — so there was nothing left to
///     notice.
///     </para>
///     <para>
///     From source, because these are about which production wins and how far it
///     gets, and a hand-built token chain fixes the first of those by
///     construction.
///     </para>
/// </remarks>
[Trait(nameof(Parser), null)]
public class Dangling
{
    private static Statement Only(string source)
    {
        Lexer lexer = new(source);
        Parser parser = new(lexer.Lex());

        return Assert.Single(parser.Parse().Scopes[0].Statements);
    }

    [Fact(DisplayName = "a datum whose type was started and abandoned")]
    public void ADatumWhoseTypeWasStartedAndAbandoned()
    {
        Assert.IsType<Datum.ExpectedTypeError>(Only("var x => = 1;"));
        Assert.IsType<Datum.ExpectedTypeError>(Only("var x => ;"));
    }

    [Fact(DisplayName = "a datum whose value was started and abandoned")]
    public void ADatumWhoseValueWasStartedAndAbandoned()
    {
        Assert.IsType<Datum.ExpectedValueError>(Only("var x = ;"));
    }

    [Fact(DisplayName = "a function whose return type was started and abandoned")]
    public void AFunctionWhoseReturnTypeWasStartedAndAbandoned()
    {
        Assert.IsType<Function.ExpectedTypeError>(Only("function f => {}"));
    }

    [Fact(DisplayName = "a type whose algebra was started and abandoned")]
    public void ATypeWhoseAlgebraWasStartedAndAbandoned()
    {
        Assert.IsType<Type.ExpectedAlgebraError>(Only("type T = ;"));
    }

    [Fact(DisplayName = "a definition whose value was started and abandoned")]
    public void ADefinitionWhoseValueWasStartedAndAbandoned()
    {
        // «=>» inside a scope introduces the value the scope evaluates to, and
        // falling through to the block form when none followed is what made
        // «function f => {}» parse at all.
        var conditional = Assert.IsType<Scope.Conditional<If>>(Only("if x => ;"));

        Assert.IsType<Scope.Definition.ExpectedValueError>(Assert.Single(conditional.Statements));
    }

    [Fact(DisplayName = "a production only commits once the statement says what it is")]
    public void AProductionOnlyCommitsOnceTheStatementSaysWhatItIs()
    {
        // «function» and «type» announce themselves with a keyword, so they are
        // committed before the «=>» is even reached. A datum has no keyword of
        // its own unless one is written, and without it the production has to be
        // free to decline — «reactive => 44.3» is not a declaration missing its
        // type, it is something else entirely.
        Lexer lexer = new("reactive => 44.3;");
        Parser parser = new(lexer.Lex());

        Assert.Null(Datum.Parse(ref parser));
    }
}
