using Ronin.Token;
using Ronin.Token.Delimiter;
using Ronin.Token.Value;
using System.Diagnostics;

namespace Feature;

public class Lexer
{
    [Fact(DisplayName = "lexing")]
    public void Lexing()
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

        function run (list of stuff => integer[], things => number) away
        {
            // this assumes the list of stuff has at least one element
            return list of stuff[0] + things * 7;
        }7aslk
        """;

        Ronin.Compiler.Lexer lexer = new(sourcecode);

        List<Lexeme> expected = new()
        {
            // datatype Weather
            Keyword(Ronin.Token.Keyword.datatype),
            Whitespace(),
            Name("Weather"),
            Whitespace(Environment.NewLine.Length),

            // {
            new OpenBrace(lexer),
            Whitespace(Environment.NewLine.Length + 4),
            
            // var data = 0b01101010010;
            Keyword(Ronin.Token.Keyword.var),
            Whitespace(),
            Name("data"),
            Whitespace(),
            new Assign(lexer),
            Whitespace(),
            new Binary(lexer, "0b01101010010".Length),
            new Terminal(lexer),
            Whitespace(Environment.NewLine.Length + 4),

            // constant sigil = 'c';
            Keyword(Ronin.Token.Keyword.constant),
            Whitespace(),
            Name("sigil"),
            Whitespace(),
            new Assign(lexer),
            Whitespace(),
            new Character(lexer, "'c'".Length),
            new Terminal(lexer),
            Whitespace(Environment.NewLine.Length + 4),

            // reactive asian sigil = '\u26fc';
            Keyword(Ronin.Token.Keyword.reactive),
            Whitespace(),
            Name("asian"),
            Whitespace(),
            Name("sigil"),
            Whitespace(),
            new Assign(lexer),
            Whitespace(),
            new Character(lexer, "'\\u26fc'".Length),
            new Terminal(lexer),
            Whitespace(Environment.NewLine.Length + 4),

            // persistent birthday = 1976-01-23;
            Keyword(Ronin.Token.Keyword.persistent),
            Whitespace(),
            Name("birthday"),
            Whitespace(),
            new Assign(lexer),
            Whitespace(),
            new Date(lexer),
            new Terminal(lexer),
            Whitespace(Environment.NewLine.Length + 4),

            // shared hex = 0x2c;
            Keyword(Ronin.Token.Keyword.shared),
            Whitespace(),
            Name("hex"),
            Whitespace(),
            new Assign(lexer),
            Whitespace(),
            new Hex(lexer, "0x2c".Length),
            new Terminal(lexer),
            Whitespace(Environment.NewLine.Length + 4),

            // compiled dogs count = 7;
            Keyword(Ronin.Token.Keyword.compiled),
            Whitespace(),
            Name("dogs"),
            Whitespace(),
            Name("count"),
            Whitespace(),
            new Assign(lexer),
            Whitespace(),
            new Integer(lexer, "7".Length),
            new Terminal(lexer),
            Whitespace(Environment.NewLine.Length + 4),

            // var cash = $14.20;
            Keyword(Ronin.Token.Keyword.var),
            Whitespace(),
            Name("cash"),
            Whitespace(),
            new Assign(lexer),
            Whitespace(),
            new Money(lexer, "$14.20".Length),
            new Terminal(lexer),
            Whitespace(Environment.NewLine.Length + 4),

            // var when = 17:24:24;
            Keyword(Ronin.Token.Keyword.var),
            Whitespace(),
            Name("when"),
            Whitespace(),
            new Assign(lexer),
            Whitespace(),
            new Time(lexer, "17:24:24".Length),
            new Terminal(lexer),
            Whitespace(Environment.NewLine.Length + 4),

            // var googles address = https://google.com;
            Keyword(Ronin.Token.Keyword.var),
            Whitespace(),
            Name("googles"),
            Whitespace(),
            Name("address"),
            Whitespace(),
            new Assign(lexer),
            Whitespace(),
            new Url(lexer, "https://google.com".Length),
            new Terminal(lexer),
            Whitespace(Environment.NewLine.Length),

            // }
            new CloseBrace(lexer),
            Whitespace(Environment.NewLine.Length * 2),

            // function run (list of stuff is integer[], things is number) away
            Keyword(Ronin.Token.Keyword.function),
            Whitespace(),
            Name("run"),
            Whitespace(),
            new OpenParenthesis(lexer),
            Name("list"),
            Whitespace(),
            Name("of"),
            Whitespace(),
            Name("stuff"),
            Whitespace(),
            new Returns(lexer),
            Whitespace(),
            Name("integer"),
            new OpenSquareBracket(lexer),
            new CloseSquareBracket(lexer),
            new Separator(lexer),
            Whitespace(),
            Name("things"),
            Whitespace(),
            new Returns(lexer),
            Whitespace(),
            Name("number"),
            new CloseParenthesis(lexer),
            Whitespace(),
            Name("away"),
            Whitespace(Environment.NewLine.Length),

            // {
            new OpenBrace(lexer),
            Whitespace(Environment.NewLine.Length + 4),

            // comment: // this assumes the list of stuff has at least one element
            new Comment(lexer, "// this assumes the list of stuff has at least one element".Length),
            Whitespace(Environment.NewLine.Length + 4),

            // return list of stuff[0] + things * 7;
            Keyword(Ronin.Token.Keyword.@return),
            Whitespace(),
            Name("list"),
            Whitespace(),
            Name("of"),
            Whitespace(),
            Name("stuff"),
            new OpenSquareBracket(lexer),
            new Integer(lexer, "0".Length),
            new CloseSquareBracket(lexer),
            Whitespace(),
            Name("+"),
            Whitespace(),
            Name("things"),
            Whitespace(),
            Name("*"),
            Whitespace(),
            new Integer(lexer, "7".Length),
            new Terminal(lexer),
            Whitespace(Environment.NewLine.Length),

            // }
            new CloseBrace(lexer),

            // 7aslk
            new Error(lexer, "7".Length),
            Name("aslk")
        };

        lexer = new(sourcecode);
        var tokens = lexer.Lex().ToArray();

        Assert.Equal(expected.Count, tokens.Length);
        for (int i = 0; i != expected.Count; ++i)
        {
            Debug.WriteLine(i);
            Assert.Equal(expected[i].GetType(), tokens[i].GetType());
            Assert.Equal(expected[i].Sourcecode.ToArray(), tokens[i].Sourcecode.ToArray());
        }
        
        Name Name(string name) => new(lexer, name.Length);
        Whitespace Whitespace(int spaces = 1) => new(lexer, spaces);
        Keyword Keyword(string word) => new(lexer, word.Length);
    }
}
