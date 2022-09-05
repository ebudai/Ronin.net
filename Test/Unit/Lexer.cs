using Ronin.Tokens;
using System.Diagnostics;

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
            var asian sigil = '\u26fc';
            var birthday = 1976-01-23;
            var hex = 0x2c;
            var dogs count = 7;
            var cash = $14.20;
            var when = 17:24:24;
            var googles address = https://google.com;
        }

        function run (list of stuff is integer[], things is number) away
        {
            // this assumes the list of stuff has at least one element
            return list of stuff[0] + things * 7;
        }
        """;

        Ronin.Compiler.Lexer lexer = new(sourcecode);

        List<Token> compiled = new()
        {
            // datatype Weather
            new Ronin.Tokens.Modifiers.Datatype(lexer),
            Whitespace(),
            Name("Weather"),
            Whitespace(Environment.NewLine.Length),

            // {
            new Ronin.Tokens.Symbols.OpenBrace(lexer),
            Whitespace(Environment.NewLine.Length + 4),
            
            // var data = 0b01101010010;
            new Ronin.Tokens.Modifiers.Variable(lexer),
            Whitespace(),
            Name("data"),
            Whitespace(),
            Name("="),
            Whitespace(),
            new Ronin.Tokens.Literals.BinaryLiteral(lexer, "0b01101010010".Length),
            new Ronin.Tokens.Symbols.Terminal(lexer),
            Whitespace(Environment.NewLine.Length + 4),

            // var sigil = 'c';
            new Ronin.Tokens.Modifiers.Constant(lexer),
            Whitespace(),
            Name("sigil"),
            Whitespace(),
            Name("="),
            Whitespace(),
            new Ronin.Tokens.Literals.CharLiteral(lexer, "'c'".Length),
            new Ronin.Tokens.Symbols.Terminal(lexer),
            Whitespace(Environment.NewLine.Length + 4),

            // var asian sigil = '\u26fc';
            new Ronin.Tokens.Modifiers.Variable(lexer),
            Whitespace(),
            Name("asian"),
            Whitespace(),
            Name("sigil"),
            Whitespace(),
            Name("="),
            Whitespace(),
            new Ronin.Tokens.Literals.CharLiteral(lexer, "'\\u26fc'".Length),
            new Ronin.Tokens.Symbols.Terminal(lexer),
            Whitespace(Environment.NewLine.Length + 4),

            // var birthday = 1976-01-23;
            new Ronin.Tokens.Modifiers.Variable(lexer),
            Whitespace(),
            Name("birthday"),
            Whitespace(),
            Name("="),
            Whitespace(),
            new Ronin.Tokens.Literals.DateLiteral(lexer, "1976-01-23".Length),
            new Ronin.Tokens.Symbols.Terminal(lexer),
            Whitespace(Environment.NewLine.Length + 4),

            // var hex = 0x2c;
            new Ronin.Tokens.Modifiers.Variable(lexer),
            Whitespace(),
            Name("hex"),
            Whitespace(),
            Name("="),
            Whitespace(),
            new Ronin.Tokens.Literals.HexLiteral(lexer, "0x2c".Length),
            new Ronin.Tokens.Symbols.Terminal(lexer),
            Whitespace(Environment.NewLine.Length + 4),

            // var dogs count = 7;
            new Ronin.Tokens.Modifiers.Variable(lexer),
            Whitespace(),
            Name("dogs"),
            Whitespace(),
            Name("count"),
            Whitespace(),
            Name("="),
            Whitespace(),
            new Ronin.Tokens.Literals.IntegerLiteral(lexer, "7".Length),
            new Ronin.Tokens.Symbols.Terminal(lexer),
            Whitespace(Environment.NewLine.Length + 4),

            // var cash = $14.20;
            new Ronin.Tokens.Modifiers.Variable(lexer),
            Whitespace(),
            Name("cash"),
            Whitespace(),
            Name("="),
            Whitespace(),
            new Ronin.Tokens.Literals.MoneyLiteral(lexer, "$14.20".Length),
            new Ronin.Tokens.Symbols.Terminal(lexer),
            Whitespace(Environment.NewLine.Length + 4),

            // var when = 17:24:24;
            new Ronin.Tokens.Modifiers.Variable(lexer),
            Whitespace(),
            Name("when"),
            Whitespace(),
            Name("="),
            Whitespace(),
            new Ronin.Tokens.Literals.TimeLiteral(lexer, "17:24:24".Length),
            new Ronin.Tokens.Symbols.Terminal(lexer),
            Whitespace(Environment.NewLine.Length + 4),

            // var googles address = https://google.com;
            new Ronin.Tokens.Modifiers.Variable(lexer),
            Whitespace(),
            Name("googles"),
            Whitespace(),
            Name("address"),
            Whitespace(),
            Name("="),
            Whitespace(),
            new Ronin.Tokens.Literals.UrlLiteral(lexer, "https://google.com".Length),
            new Ronin.Tokens.Symbols.Terminal(lexer),
            Whitespace(Environment.NewLine.Length),

            // }
            new Ronin.Tokens.Symbols.CloseBrace(lexer),
            Whitespace(Environment.NewLine.Length * 2),

            // function run (list of stuff is integer[], things is number) away
            new Ronin.Tokens.Modifiers.Function(lexer),
            Whitespace(),
            Name("run"),
            Whitespace(),
            new Ronin.Tokens.Symbols.OpenParenthesis(lexer),
            Name("list"),
            Whitespace(),
            Name("of"),
            Whitespace(),
            Name("stuff"),
            Whitespace(),
            Name("is"),
            Whitespace(),
            Name("integer"),
            new Ronin.Tokens.Symbols.OpenSquareBracket(lexer),
            new Ronin.Tokens.Symbols.CloseSquareBracket(lexer),
            new Ronin.Tokens.Symbols.Separator(lexer),
            Whitespace(),
            Name("things"),
            Whitespace(),
            Name("is"),
            Whitespace(),
            Name("number"),
            new Ronin.Tokens.Symbols.CloseParenthesis(lexer),
            Whitespace(),
            Name("away"),
            Whitespace(Environment.NewLine.Length),

            // {
            new Ronin.Tokens.Symbols.OpenBrace(lexer),
            Whitespace(Environment.NewLine.Length + 4),

            // comment: // this assumes the list of stuff has at least one element
            new Ronin.Tokens.Comment(lexer, "// this assumes the list of stuff has at least one element".Length),
            Whitespace(Environment.NewLine.Length + 4),

            // return list of stuff[0] + things * 7;
            Name("return"),
            Whitespace(),
            Name("list"),
            Whitespace(),
            Name("of"),
            Whitespace(),
            Name("stuff"),
            new Ronin.Tokens.Symbols.OpenSquareBracket(lexer),
            new Ronin.Tokens.Literals.IntegerLiteral(lexer, "0".Length),
            new Ronin.Tokens.Symbols.CloseSquareBracket(lexer),
            Whitespace(),
            Name("+"),
            Whitespace(),
            Name("things"),
            Whitespace(),
            Name("*"),
            Whitespace(),
            new Ronin.Tokens.Literals.IntegerLiteral(lexer, "7".Length),
            new Ronin.Tokens.Symbols.Terminal(lexer),
            Whitespace(Environment.NewLine.Length),

            // }
            new Ronin.Tokens.Symbols.CloseBrace(lexer),
        };

        lexer = new(sourcecode);
        var tokens = lexer.Lex();

        Assert.Equal(compiled.Count, tokens.Count);
        for (int i = 0; i != compiled.Count; ++i)
        {
            Debug.WriteLine(i);
            Assert.Equal(compiled[i].GetType(), tokens[i].GetType());
            Assert.Equal(compiled[i].Sourcecode.ToArray(), tokens[i].Sourcecode.ToArray());
        }
        
        Name Name(string name) => new(lexer, name.Length);
        Ronin.Tokens.Whitespace Whitespace(int spaces = 1) => new Ronin.Tokens.Whitespace(lexer, spaces);
    }
}
