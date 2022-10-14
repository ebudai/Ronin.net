using Ronin.Compiler;

namespace Unit;

public class Object
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string declaration = "(test)";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();


    }
}
