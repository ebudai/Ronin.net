using Ronin.Token;
using Ronin.Token.Delimiter;
using Ronin.Token.Value;
using System.Diagnostics;
using System.Reflection;

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
            Binary("0b01101010010"),
            new Terminal(lexer),
            Whitespace(Environment.NewLine.Length + 4),

            // constant sigil = 'c';
            Keyword(Ronin.Token.Keyword.constant),
            Whitespace(),
            Name("sigil"),
            Whitespace(),
            new Assign(lexer),
            Whitespace(),
            Character("'c'"),
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
            Character("'\\u26fc'"),
            new Terminal(lexer),
            Whitespace(Environment.NewLine.Length + 4),

            // persistent birthday = 1976-01-23;
            Keyword(Ronin.Token.Keyword.persistent),
            Whitespace(),
            Name("birthday"),
            Whitespace(),
            new Assign(lexer),
            Whitespace(),
            Date(),
            new Terminal(lexer),
            Whitespace(Environment.NewLine.Length + 4),

            // shared hex = 0x2c;
            Keyword(Ronin.Token.Keyword.shared),
            Whitespace(),
            Name("hex"),
            Whitespace(),
            new Assign(lexer),
            Whitespace(),
            Hex("0x2c"),
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
            Integer("7"),
            new Terminal(lexer),
            Whitespace(Environment.NewLine.Length + 4),

            // var cash = $14.20;
            Keyword(Ronin.Token.Keyword.var),
            Whitespace(),
            Name("cash"),
            Whitespace(),
            new Assign(lexer),
            Whitespace(),
            Money("$14.20"),
            new Terminal(lexer),
            Whitespace(Environment.NewLine.Length + 4),

            // var when = 17:24:24;
            Keyword(Ronin.Token.Keyword.var),
            Whitespace(),
            Name("when"),
            Whitespace(),
            new Assign(lexer),
            Whitespace(),
            Time("17:24:24"),
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
            Url("https://google.com"),
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
            Integer("0"),
            new CloseSquareBracket(lexer),
            Whitespace(),
            Name("+"),
            Whitespace(),
            Name("things"),
            Whitespace(),
            Name("*"),
            Whitespace(),
            Integer("7"),
            new Terminal(lexer),
            Whitespace(Environment.NewLine.Length),

            // }
            new CloseBrace(lexer),

            // 7aslk
            Integer("7"),
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
        Binary Binary(string value) => BinaryConstructor.Invoke(new object[] { lexer, value.Length }) as Binary;
        Character Character(string value) => CharacterConstructor.Invoke(new object[] { lexer, value.Length }) as Character;
        Date Date() => DateConstructor.Invoke(new object[] { lexer }) as Date;
        Hex Hex(string value) => HexConstructor.Invoke(new object[] { lexer, value.Length }) as Hex;
        Integer Integer(string value) => IntegerConstructor.Invoke(new object[] { lexer, value.Length }) as Integer;
        Money Money(string value) => MoneyConstructor.Invoke(new object[] { lexer, value.Length }) as Money;
        Time Time(string value) => TimeConstructor.Invoke(new object[] { lexer, value.Length }) as Time;
        Url Url(string value) => UrlConstructor.Invoke(new object[] { lexer, value.Length }) as Url;

    }

    private static readonly ConstructorInfo BinaryConstructor = typeof(Binary).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(Ronin.Compiler.Lexer), typeof(int) });
    private static readonly ConstructorInfo CharacterConstructor = typeof(Character).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(Ronin.Compiler.Lexer), typeof(int) });
    private static readonly ConstructorInfo DateConstructor = typeof(Date).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(Ronin.Compiler.Lexer) });
    private static readonly ConstructorInfo HexConstructor = typeof(Hex).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(Ronin.Compiler.Lexer), typeof(int) });
    private static readonly ConstructorInfo IntegerConstructor = typeof(Integer).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(Ronin.Compiler.Lexer), typeof(int) });
    private static readonly ConstructorInfo MoneyConstructor = typeof(Money).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(Ronin.Compiler.Lexer), typeof(int) });
    private static readonly ConstructorInfo TimeConstructor = typeof(Time).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(Ronin.Compiler.Lexer), typeof(int) });
    private static readonly ConstructorInfo UrlConstructor = typeof(Url).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(Ronin.Compiler.Lexer), typeof(int) });

}
