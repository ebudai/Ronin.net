using Ronin.Compiler;
using Ronin.Lexicon;

namespace Unit;

[Trait("Parser", null)]
public class Parsing
{
    [Fact(DisplayName = "parse")]
    public void Parse()
    {
        Token[] tokens =
        {
            // part of testing apparatus;

            new PartOfKeyword(),
            new Word(),
            new Word(),
            new TerminalSymbol(),

            // var a = 3;

            new VariableKeyword(),
            new Word(),
            new AssignSymbol(),
            new NumberLiteral(),
            new TerminalSymbol(),

            // a = 6;

            new Word(),
            new AssignSymbol(),
            new NumberLiteral(),
            new TerminalSymbol(),

            // 3..test;

            new NumberLiteral(),
            new RangeSymbol(),
            new Word(),
            new TerminalSymbol(),

            // function x (var a => number) y (cash on hand => money) { return cash on hand * a; }

            new FunctionKeyword(),
            new Word(),
            new OpenParenthesisSymbol(),
            new VariableKeyword(),
            new Word(),
            new ReturnsSymbol(),
            new Word(),
            new CloseParenthesisSymbol(),
            new Word(),
            new OpenParenthesisSymbol(),
            new Word(),
            new Word(),
            new Word(),
            new ReturnsSymbol(),
            new Word(),
            new CloseParenthesisSymbol(),
            new OpenBraceSymbol(),
            new Word(),
            new Word(),
            new Word(),
            new Word(),
            new AsteriskSymbol(),
            new Word(),
            new TerminalSymbol(),
            new CloseBraceSymbol(),

            // datatype big thing { constant size => whole number; }

            new DatatypeKeyword(),
            new Word(),
            new Word(),
            new OpenBraceSymbol(),
            new ConstantKeyword(),
            new Word(),
            new ReturnsSymbol(),
            new Word(),
            new Word(),
            new TerminalSymbol(),
            new CloseBraceSymbol(),

            // 7;

            new NumberLiteral(),
            new TerminalSymbol(),

            // (a, b, "text");

            new OpenParenthesisSymbol(),
            new Word(),
            new SeparatorSymbol(),
            new Word(),
            new SeparatorSymbol(),
            new TextLiteral(),
            new CloseParenthesisSymbol(),
            new TerminalSymbol(),

            // { var x => moment; florb x now; }

            new OpenBraceSymbol(),
            new VariableKeyword(),
            new Word(),
            new ReturnsSymbol(),
            new Word(),
            new TerminalSymbol(),
            new Word(),
            new Word(),
            new Word(),
            new TerminalSymbol(),
            new CloseBraceSymbol(),

            Sentinel.Instance
        };

        Parser parser = new(tokens);
        var statements = parser.Parse().Values;

        Assert.Equal(9, statements?.Count);

        Ronin.Grammar.ImportExportSyntax partof = statements[0];
        Assert.NotNull(partof);

        Ronin.Grammar.DatumDeclarationSyntax datum = statements[1];
        Assert.NotNull(datum);

        Ronin.Grammar.AssignmentSyntax assignment = statements[2];
        Assert.NotNull(assignment);

        Ronin.Grammar.IntervalSyntax interval = statements[3];
        Assert.NotNull(interval);

        Ronin.Grammar.FunctionDeclarationSyntax function = statements[4];
        Assert.NotNull(function);

        Ronin.Grammar.DatatypeDeclarationSyntax datatype = statements[5];
        Assert.NotNull(datatype);

        Ronin.Grammar.Value scalar_value = statements[6];
        Ronin.Grammar.LiteralSyntax scalar = scalar_value;
        Assert.NotNull(scalar);

        Ronin.Grammar.Value arguments_value = statements[7];
        Ronin.Grammar.Aggregates.Arguments arguments = arguments_value;
        Assert.NotNull(arguments);

        Ronin.Grammar.Aggregates.Scope scope = statements[8];
        Assert.NotNull(scope);
    }
}
