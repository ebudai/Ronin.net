using Ronin.Transpiler.Statements;

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
    public void DeclareVariables()
    {
        var lines = File.ReadAllLines("declare vars.ronin");
        var tokens = Lexer.Lex(lines);
        Parser parser = new();
        var statements = parser.Parse(tokens);
        Assert.Equal(7, statements.Length);

        // implicit tests
        {
            Assert.IsType<DeclareVariableImplicit>(statements[0]);
            var @implicit = statements[0] as DeclareVariableImplicit;
            Assert.Equal("x", @implicit.Name);
            Assert.Equal("3", @implicit.Statement.ToString());
            Assert.IsType<Literal>(@implicit.Statement);

            Assert.IsType<DeclareVariableImplicit>(statements[1]);
            @implicit = statements[1] as DeclareVariableImplicit;
            Assert.Equal("this is my var", @implicit.Name);
            Assert.Equal("\"12\"", @implicit.Statement.ToString());
            Assert.IsType<Literal>(@implicit.Statement);

            Assert.IsType<DeclareVariableImplicit>(statements[2]);
            @implicit = statements[2] as DeclareVariableImplicit;
            Assert.Equal("identifier test", @implicit.Name);
            Assert.Equal("some identifier", @implicit.Statement.ToString());
            Assert.IsType<Identifier>(@implicit.Statement);

            Assert.IsType<DeclareVariableImplicit>(statements[5]);
            @implicit = statements[5] as DeclareVariableImplicit;
            Assert.Equal("dot test", @implicit.Name);
            Assert.Equal("12.3", @implicit.Statement.ToString());
            Assert.IsType<Literal>(@implicit.Statement);
        }

        // explicit tests
        {
            Assert.IsType<DeclareVariableExplicit>(statements[3]);
            var @explicit = statements[3] as DeclareVariableExplicit;
            Assert.Equal("new", @explicit.Name);
            Assert.Equal("int", @explicit.Type);
            Assert.Null(@explicit.Statement);

            Assert.IsType<DeclareVariableExplicit>(statements[4]);
            @explicit = statements[4] as DeclareVariableExplicit;
            Assert.Equal("new2", @explicit.Name);
            Assert.Equal("text", @explicit.Type);
            Assert.Null(@explicit.Statement);

            Assert.IsType<DeclareVariableExplicit>(statements[6]);
            @explicit = statements[6] as DeclareVariableExplicit;
            Assert.Equal("thingy", @explicit.Name);
            Assert.Equal("int", @explicit.Type);
            Assert.Equal("6", @explicit.Statement.ToString());
            Assert.IsType<Literal>(@explicit.Statement);
        }
    }
}