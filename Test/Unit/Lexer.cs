using Ronin.Token;
using System.Diagnostics;

using static Ronin.Token.Keyword.Word;
using static Ronin.Token.Literal.Kind;

namespace Unit;

public class Lexer
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string sourcecode =
        """
        datatype Weather
        {
            var data = 0b01101010010;
            constant sigil = 'c';
            reactive asian sigil = '\u26fc';
            persistent birthday = 1976-01-23;
            shared hex = 0x2c;
            compiled dogs count = 7;
            var cash = $14.20;
            var when = 17:24:24;
            var googles address = https://google.com;
        }

        function run (list of stuff is integer[], things is number) away
        {
            // this assumes the list of stuff has at least one element
            return list of stuff[0] + things * 7;
        }7aslk
        """;

        Ronin.Compiler.Lexer lexer = new(sourcecode);

        List<Token> expected = new()
        {
            // datatype Weather
            Keyword(datatype),
            Whitespace(),
            Name("Weather"),
            Whitespace(Environment.NewLine.Length),

            // {
            Symbol("{"),
            Whitespace(Environment.NewLine.Length + 4),
            
            // var data = 0b01101010010;
            Keyword(var),
            Whitespace(),
            Name("data"),
            Whitespace(),
            Name("="),
            Whitespace(),
            new Literal(lexer, "0b01101010010".Length, binary),
            Symbol(";"),
            Whitespace(Environment.NewLine.Length + 4),

            // constant sigil = 'c';
            Keyword(constant),
            Whitespace(),
            Name("sigil"),
            Whitespace(),
            Name("="),
            Whitespace(),
            new Literal(lexer, "'c'".Length, character),
            Symbol(";"),
            Whitespace(Environment.NewLine.Length + 4),

            // reactive asian sigil = '\u26fc';
            Keyword(reactive),
            Whitespace(),
            Name("asian"),
            Whitespace(),
            Name("sigil"),
            Whitespace(),
            Name("="),
            Whitespace(),
            new Literal(lexer, "'\\u26fc'".Length, character),
            Symbol(";"),
            Whitespace(Environment.NewLine.Length + 4),

            // persistent birthday = 1976-01-23;
            Keyword(persistent),
            Whitespace(),
            Name("birthday"),
            Whitespace(),
            Name("="),
            Whitespace(),
            new Literal(lexer, "1976-01-23".Length, date),
            Symbol(";"),
            Whitespace(Environment.NewLine.Length + 4),

            // shared hex = 0x2c;
            Keyword(shared),
            Whitespace(),
            Name("hex"),
            Whitespace(),
            Name("="),
            Whitespace(),
            new Literal(lexer, "0x2c".Length, hex),
            Symbol(";"),
            Whitespace(Environment.NewLine.Length + 4),

            // compiled dogs count = 7;
            Keyword(compiled),
            Whitespace(),
            Name("dogs"),
            Whitespace(),
            Name("count"),
            Whitespace(),
            Name("="),
            Whitespace(),
            new Literal(lexer, "7".Length, integer),
            Symbol(";"),
            Whitespace(Environment.NewLine.Length + 4),

            // var cash = $14.20;
            Keyword(var),
            Whitespace(),
            Name("cash"),
            Whitespace(),
            Name("="),
            Whitespace(),
            new Literal(lexer, "$14.20".Length, money),
            Symbol(";"),
            Whitespace(Environment.NewLine.Length + 4),

            // var when = 17:24:24;
            Keyword(var),
            Whitespace(),
            Name("when"),
            Whitespace(),
            Name("="),
            Whitespace(),
            new Literal(lexer, "17:24:24".Length, time),
            Symbol(";"),
            Whitespace(Environment.NewLine.Length + 4),

            // var googles address = https://google.com;
            Keyword(var),
            Whitespace(),
            Name("googles"),
            Whitespace(),
            Name("address"),
            Whitespace(),
            Name("="),
            Whitespace(),
            new Literal(lexer, "https://google.com".Length, url),
            Symbol(";"),
            Whitespace(Environment.NewLine.Length),

            // }
            Symbol("}"),
            Whitespace(Environment.NewLine.Length * 2),

            // function run (list of stuff is integer[], things is number) away
            Keyword(function),
            Whitespace(),
            Name("run"),
            Whitespace(),
            Symbol("("),
            Name("list"),
            Whitespace(),
            Name("of"),
            Whitespace(),
            Name("stuff"),
            Whitespace(),
            Name("is"),
            Whitespace(),
            Name("integer"),
            Symbol("["),
            Symbol("]"),
            Symbol(","),
            Whitespace(),
            Name("things"),
            Whitespace(),
            Name("is"),
            Whitespace(),
            Name("number"),
            Symbol(")"),
            Whitespace(),
            Name("away"),
            Whitespace(Environment.NewLine.Length),

            // {
            Symbol("{"),
            Whitespace(Environment.NewLine.Length + 4),

            // comment: // this assumes the list of stuff has at least one element
            new Ronin.Token.Comment(lexer, "// this assumes the list of stuff has at least one element".Length),
            Whitespace(Environment.NewLine.Length + 4),

            // return list of stuff[0] + things * 7;
            Keyword(@return),
            Whitespace(),
            Name("list"),
            Whitespace(),
            Name("of"),
            Whitespace(),
            Name("stuff"),
            Symbol("["),
            new Literal(lexer, "0".Length, integer),
            Symbol("]"),
            Whitespace(),
            Name("+"),
            Whitespace(),
            Name("things"),
            Whitespace(),
            Name("*"),
            Whitespace(),
            new Literal(lexer, "7".Length, integer),
            Symbol(";"),
            Whitespace(Environment.NewLine.Length),

            // }
            Symbol("}"),

            // 7aslk
            new Ronin.Token.Error(lexer, "7".Length),
            Name("aslk")
        };

        lexer = new(sourcecode);
        var tokens = lexer.Lex();

        Assert.Equal(expected.Count, tokens.Count);
        for (int i = 0; i != expected.Count; ++i)
        {
            Debug.WriteLine(i);
            Assert.Equal(expected[i].GetType(), tokens[i].GetType());
            Assert.Equal(expected[i].Sourcecode.ToArray(), tokens[i].Sourcecode.ToArray());
        }
        
        Ronin.Token.Name Name(string name) => new(lexer, name.Length);
        Ronin.Token.Whitespace Whitespace(int spaces = 1) => new(lexer, spaces);
        Ronin.Token.Symbol Symbol(string symbol) => new(lexer, symbol.Length);
        Ronin.Token.Keyword Keyword(Ronin.Token.Keyword.Word word) => new(lexer, Enum.GetName(word).Length);
    }
}
