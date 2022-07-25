using Ronin.Transpiler.Program;

namespace Ronin.Transpiler.Test;

public class Unit
{
    [Fact]
    public void LexLiterals()
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
    public void Package()
    {
        var lines = File.ReadAllLines("package.ronin");
        ReadOnlySpan<Token> tokens = Lexer.Lex(lines);
        Parser parser = new();
        Block block = new();
        var statement = parser.Parse(tokens, block);
        Assert.IsType<PackageStatement>(statement);
        Assert.Equal("ronin best programming language!!", block.Name);
    }

    [Fact]
    public void DeclareVariables()
    {
        var lines = File.ReadAllLines("declare vars.ronin");
        var tokens = Lexer.Lex(lines);
        Parser parser = new();
        Block block = new();
        while (tokens.Length is > 0)
        {
            var statement = parser.Parse(tokens, block, out var index);
            block.Statements.Add(statement);
            tokens = tokens[index..];
        }        
        Assert.Equal(11, block.Statements.Count);

        Assert.IsType<DeclareVariableStatement>(block.Statements[0]);
        Assert.Contains("x", block.Data.Keys);

        // implicit tests
        {
            /*Assert.IsType<DeclareVariableStatement>(block.Statements[0]);
            Assert.Contains("x", block.Data.Keys);
            var x = block.Data["x"];
            Assert.Equal("3", block.Data["x"].Initializer)
            var declare = block.Statements[0] as DeclareVariable;
            Assert.Equal("x", declare.Name);
            Assert.Equal("3", declare.Initializer.ToString());
            Assert.IsType<Literal>(declare.Initializer);

            Assert.IsType<DeclareVariableImplicit>(block[1]);
            declare = block[1] as DeclareVariableImplicit;
            Assert.Equal("this is my var", declare.Name);
            Assert.Equal("\"12\"", declare.Initializer.ToString());
            Assert.IsType<Literal>(declare.Initializer);

            Assert.IsType<DeclareVariableImplicit>(block[2]);
            declare = block[2] as DeclareVariableImplicit;
            Assert.Equal("identifier test", declare.Name);
            Assert.Equal("some identifier", declare.Initializer.ToString());
            Assert.IsType<Identifier>(declare.Initializer);

            Assert.IsType<DeclareVariableImplicit>(block[5]);
            declare = block[5] as DeclareVariableImplicit;
            Assert.Equal("dot test", declare.Name);
            Assert.Equal("12.3", declare.Initializer.ToString());
            Assert.IsType<Literal>(declare.Initializer);*/
        }

        // explicit tests
        /*{
            Assert.IsType<DeclareVariableExplicit>(block[3]);
            var @explicit = block[3] as DeclareVariableExplicit;
            Assert.Equal("new", @explicit.Name);
            Assert.Equal("int", @explicit.Type);
            Assert.Null(@explicit.Statement);

            Assert.IsType<DeclareVariableExplicit>(block[4]);
            @explicit = block[4] as DeclareVariableExplicit;
            Assert.Equal("new2", @explicit.Name);
            Assert.Equal("text", @explicit.Type);
            Assert.Null(@explicit.Statement);

            Assert.IsType<DeclareVariableExplicit>(block[6]);
            @explicit = block[6] as DeclareVariableExplicit;
            Assert.Equal("thingy", @explicit.Name);
            Assert.Equal("int", @explicit.Type);
            Assert.Equal("6", @explicit.Statement.ToString());
            Assert.IsType<Literal>(@explicit.Statement);
        }*/
    }
}