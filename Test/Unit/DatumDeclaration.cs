using Ronin.Grammar;

namespace Unit;

public class Datum
{
    [Fact(DisplayName = "typed")]
    public void Typed()
    {
        const string declaration = "var my variable => integer;";

        Ronin.Compiler.Lexer lexer = new(declaration);
        var tokens = lexer.Lex().ToArray();
        Ronin.Grammar.Datum datum = new();
        
        var result = datum.Add(tokens[0]);
        Assert.Equal(Syntax.Result.Applied, result);

        result = datum.Add(tokens[2]);
        Assert.Equal(Syntax.Result.Applied, result);

        result = datum.Add(tokens[4]);
        Assert.Equal(Syntax.Result.Applied, result);

        result = datum.Add(tokens[6]);
        Assert.Equal(Syntax.Result.Descended, result);
    }
}
