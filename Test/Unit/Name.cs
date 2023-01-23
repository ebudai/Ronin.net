using Ronin.Compiler;
using Ronin.Grammar;

namespace Unit;

[Trait("Parser", null)]
public class Name
{
    [Fact(DisplayName = "symbols")]
    public void Symbols()
    {
        const string code = "name+things;";

        Lexer lexer = new(code);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var result = parser.Parse();

        Assert.NotEmpty(result);
        Reference reference = result[0] as Statement;
        Assert.NotNull(reference);
        Assert.NotEmpty(reference.Components);
        Ronin.Grammar.Name name = reference.Components[0];
        Assert.Equal(3, name.Words.Count);
        Assert.Equal("name", name.Words[0]);
        Assert.Equal("+", name.Words[1]);
        Assert.Equal("things", name.Words[2]);
    }

    /*[Fact(DisplayName = "transpile")]
    public void Transpile() 
    {
        const string code = "my variable";

        Lexer lexer = new(code);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var result = parser.Parse();

        Assert.NotEmpty(result);
        Reference reference = result[0] as Statement;
        Assert.NotNull(reference);
        Assert.NotEmpty(reference.Values);
        Ronin.Grammar.Name name = reference.Values[0];
        
        var transpiled = name.ToString();
        Assert.Equal("my_variable", transpiled);
    }*/
}
