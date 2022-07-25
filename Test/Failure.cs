using Ronin.Transpiler.Program;

namespace Ronin.Transpiler.Test;

public class Failure
{
    [Fact]
    public void KeywordsInVariableNames()
    {   
        Assert.Throws<Parser.Exception>(Parse);

        static void Parse()
        {
            var lines = File.ReadAllLines("bad.ronin");
            ReadOnlySpan<Token> tokens = Lexer.Lex(lines);
            Parser parser = new();
            Block block = new();
            parser.Parse(tokens, block);
        }
    }
}
