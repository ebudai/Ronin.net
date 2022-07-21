using Ronin.Transpiler;

namespace Ronin.Test.Transpiler;

public class Unit
{
    [Fact]
    public void LexLiterals()
    {
        FileInfo file = new(@"Code\literals.ronin");
        var lines = File.ReadAllLines(file.FullName);
        var tokens = Lexer.Lex(lines);
        Assert.Equal(lines.Length, tokens.Length);
    }

    [Fact]
    public void LexDeclarations()
    {
        FileInfo file = new(@"Code\declarations.ronin");
        var tokens = Lexer.Lex(File.ReadAllLines(file.FullName));
        Assert.Equal(4, tokens.Length);
    }
}