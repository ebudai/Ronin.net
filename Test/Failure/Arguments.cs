using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Errors;
using Ronin.Lexicon.Symbols;

namespace Failure;

[Trait("Parser", null)]
public class Arguments
{
    [Fact(DisplayName = "does not start with (")]
    public void NotAnObject()
    {
        const string sourcecode = "not an object;";

        Lexer lexer = new(sourcecode);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var aggregate = Ronin.Grammar.Aggregates.Arguments.Parse(ref parser);

        Assert.Null(aggregate);
    }

    [Fact(DisplayName = "blank")]
    public void Blank()
    {
        const string sourcecode = "";

        Lexer lexer = new(sourcecode);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.Empty(syntax);
    }

    [Fact(DisplayName = "recursive bad syntax")]
    public void RecursiveBadSyntax()
    {
        const string sourcecode = "(test, (thing;stuff));";

        Lexer lexer = new(sourcecode);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.Empty(syntax);
        Assert.NotEmpty(parser.Errors);
        Assert.IsType<ExpectedSyntaxError<Separator, CloseParenthesis>>(parser.Errors[0]);
    }

    [Fact(DisplayName = "terminated incorrectly")]
    public void TerminatedWrong()
    {
        const string sourcecode = "(test;);";

        Lexer lexer = new(sourcecode);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var statements = parser.Parse();

        Assert.Empty(statements);
        Assert.NotEmpty(parser.Errors);
        Assert.IsType<ExpectedSyntaxError<Separator, CloseParenthesis>>(parser.Errors[0]);
    }
}
