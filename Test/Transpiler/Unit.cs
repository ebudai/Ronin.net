using Ronin.Transpiler;

namespace Ronin.Test.Transpiler;

public class Unit
{
    [Fact]
    public void ParseIncludeStatement()
    {
        FileInfo file = new(@"Code\statements.ronin");
        var tokens = Lexer.GetTokens(File.ReadAllLines(file.FullName));
        Assert.Equal(2, tokens.Length);
    }

    [Fact]
    public void LexLiterals()
    {
        FileInfo file = new(@"Code\literals.ronin");
        var lines = File.ReadAllLines(file.FullName);
        var tokens = Lexer.GetTokens(lines);
        Assert.Equal(lines.Length, tokens.Length);
    }

    [Fact]
    public void ParseTypeDeclareStatement()
    {
        FileInfo file = new(@"Code\declarations.ronin");
        var tokens = Lexer.GetTokens(File.ReadAllLines(file.FullName));
        Assert.Equal(4, tokens.Length);
    }
}