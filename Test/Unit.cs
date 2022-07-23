namespace Ronin.Transpiler.Test;

public class Unit
{
    [Fact]
    public void Lexing()
    {
        string[] matches =
        {
            "~",
            "\"-0x55ce\"",
            "\"testtest\"",
            "\"'c'\"",
            "\"test\\\"test\"",
            "'c'",
            @"'\0'",
            @"'\u7b43'",
            @"'\uAABC'",
            "0x17",
            "0xa40",
            "0xABCDE",
            "0XaBFcd",
            "0X886",
            "0x44",
            "0x55ce",
            "0b110101",
            "0B110",
            "0b1001",
            "0b_100__10001",
            "0B010_001",
            "107.2",
            "33.3",
            "18r",
            "33R",
            "55534R32",
            "343r16",
            "3443.222R16",
            "234059.23r64",
            "$125.33",
            "$8",
            "$405.2342323",
            "2345",
            "1",
            "0",
            "15i8",
            "16i",
            "666i16",
            "12888I",
            "4I32",
            "8000I64",
        };
        var lines = File.ReadAllLines("lexing.ronin");
        var tokens = Lexer.Lex(lines);

        Assert.Equal(matches.Length, tokens.Length);

        // ensure whitespace 
        Assert.Equal(lines[0].Length - 1, tokens[0].Column);
        
        // match tokens
        for (int i = 0; i != matches.Length; ++i)
        {
            Assert.Equal(matches[i], tokens[i].Value);
            Assert.Equal(i, tokens[i].Line);
            if (i is not 0) Assert.Equal(0, tokens[i].Column);
        }
    }

    [Fact]
    public void Declarations()
    {
        var lines = File.ReadAllLines("declare.ronin");
        var tokens = Lexer.Lex(lines);
        Parser parser = new();
        var statements = parser.Parse(tokens);
        Assert.Equal(2, statements.Length);
    }
}