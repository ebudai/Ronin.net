// Copyright © 2026 Eric Budai

using Ronin.Compiler;
using Ronin.Grammar;

namespace Integration;

/// <summary>
///     The property no single production can be trusted with: a parse always
///     finishes, and always accounts for its input.
/// </summary>
///
/// <remarks>
///     <para>
///     From source through the real lexer, because the defect this pins could
///     not be reached from a hand-built token chain: every failure test above
///     calls one production once and asserts the node it returns, and the bug
///     was that the production left its CALLER where it was. That only shows up
///     when something loops over statements.
///     </para>
///     <para>
///     «var +;» did not hang, it grew. <c>Module.Parse</c> reparsed «var»
///     forever and appended a fresh error statement each time round, so a
///     one-line file was an out-of-memory: the audit's own copy reached 98 GiB
///     before it was killed. A timeout alone would have called that a hang and
///     missed the more serious half.
///     </para>
/// </remarks>
[Trait(nameof(Parser), null)]
public class Progress
{
    /// <summary>
    ///     Malformed input, one line each: a keyword with nothing after it, a
    ///     keyword followed by the wrong thing, and dangling operators.
    /// </summary>
    public static TheoryData<string> Malformed =>
    [
        "var +;",
        "var ;",
        "var",
        "var 555;",
        "var x =;",
        "var x => ;",
        "var x => = 1;",
        "function ;",
        "function ;;;",
        "function f => {}",
        "function f",
        "type ;",
        "type T = ;",
        "type T =",
        "part of ;",
        "part of 555;",
        "import ;",
        "import 555;",
        "if ;",
        "while ;",
        "when ;",
        "when changing ;",
        "for ;",
        "for each ;",
        "= 5;",
        "+;",
        ";;;;",
        "}",
        ")",
        ",",
        "(",
        "{",
        "var x = (;",
        "var x = );",
        "reactive => 44.3;",
    ];

    [Theory(DisplayName = "a malformed statement still finishes, and is still accounted for")]
    [MemberData(nameof(Malformed))]
    public void AMalformedStatementStillFinishes(string source)
    {
        Lexer lexer = new(source);
        Parser parser = new(lexer.Lex());

        var module = parser.Parse();

        // Every token is either inside a statement or inside the unexpected-input
        // node, and there is no third place for one to go. Stopping where
        // statements stop used to discard the rest of the file in silence.
        Assert.False(parser.IsNotFinished, $"«{source}» left input unconsumed");
        Assert.NotEmpty(module.Scopes);
    }

    [Fact(DisplayName = "a production that returns a node has consumed one")]
    public void AProductionThatReturnsANodeHasConsumedOne()
    {
        // The invariant itself, at the one production that broke it. An error
        // node is still a successful parse as far as the caller is concerned, so
        // it owes the caller the same progress a real node does.
        const string source = "var +;";

        Lexer lexer = new(source);
        Parser parser = new(lexer.Lex());

        var before = parser.Token;
        var datum = Datum.Parse(ref parser);

        Assert.IsType<Datum.ExpectedIdentifierError>(datum);
        Assert.NotSame(before, parser.Token);
    }

    [Fact(DisplayName = "recovery runs to the end of the statement, not to the first surprise")]
    public void RecoveryRunsToTheEndOfTheStatement()
    {
        // One mistake is one error. Stopping at the token that failed left the
        // rest of the line to be parsed as though it were a statement of its
        // own, so «part of 555» reported a missing name AND a stray number.
        const string source = "part of 555 666;\nvar total = 3;";

        Lexer lexer = new(source);
        Parser parser = new(lexer.Lex());

        var statements = parser.Parse().Scopes[0].Statements;

        Assert.Equal(2, statements.Count);
        Assert.IsType<Export.ExpectedNameError>(statements[0]);

        // and the statement after the bad one parses normally, which is the
        // point of resynchronising on the terminator
        Assert.IsType<Datum>(statements[1]);
    }
}
