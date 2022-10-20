using Ronin.Compiler;
using Ronin.Token.Symbols;
using Ronin.Grammar;

namespace Failure;

public class Aggregate
{
    [Fact(DisplayName = "does not start with (")]
    public void NotAnObject()
    {
        const string sourcecode = "not an object";

        Lexer lexer = new(sourcecode);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var aggregate = Aggregate.Parse(parser);

        Assert.IsType<Expected<OpenParenthesis>>(aggregate);
    }

    [Fact(DisplayName = "blank")]
    public void Blank()
    {
        const string sourcecode = "";

        Lexer lexer = new(sourcecode);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var aggregate = Aggregate.Parse(parser);

        Assert.IsType<Expected<OpenParenthesis>>(aggregate);
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

    [Fact(DisplayName = "unterminated")]
    public void Unterminated()
    {
        const string sourcecode = "(test;);";

        Lexer lexer = new(sourcecode);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var aggregate = Aggregate.Parse(parser);

        Assert.IsAssignableFrom<Unexpected>(aggregate);
    }
}
