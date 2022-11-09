using Ronin.Compiler;
using Ronin.Grammar;

namespace Failure;

public class Arguments
{
    [Fact(DisplayName = "does not start with (")]
    public void NotAnObject()
    {
        const string sourcecode = "not an object;";

        Lexer lexer = new(sourcecode);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var aggregate = Ronin.Grammar.Arguments.Parse(ref parser);

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

    /*[Fact(DisplayName = "recursive bad syntax")]
    public void RecursiveBadSyntax()
    {
        const string sourcecode = "(test, (thing;stuff));";

        Lexer lexer = new(sourcecode);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsAssignableFrom<Expected>(syntax[^1]);
    }*/

    [Fact(DisplayName = "terminated incorrectly")]
    public void TerminatedWrong()
    {
        const string sourcecode = "(test;);";

        Lexer lexer = new(sourcecode);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var statements = parser.Parse();
        Assert.NotNull(statements);
        Assert.IsType<Error>(statements[0]);
    }
}
