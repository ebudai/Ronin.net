using Ronin.Lexicon;
using Ronin.Lexicon.Symbols;
using Ronin.Lexicon.Reserved;
using Ronin.Lexicon.Literals;
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

        List<Token> expected = new()
        {
            // datatype Weather
            Datatype(),
            Whitespace(),
            Name("Weather"),
            Whitespace(Environment.NewLine.Length),

            // {
            OpenBrace(),
            Whitespace(Environment.NewLine.Length + 4),
            
            // var data = 0b01101010010;
            Variable(),
            Whitespace(),
            Name("data"),
            Whitespace(),
            Assign(),
            Whitespace(),
            Binary("0b01101010010"),
            Terminal(),
            Whitespace(Environment.NewLine.Length + 4),

            // constant sigil = 'c';
            Constant(),
            Whitespace(),
            Name("sigil"),
            Whitespace(),
            Assign(),
            Whitespace(),
            Character("'c'"),
            Terminal(),
            Whitespace(Environment.NewLine.Length + 4),

            // reactive asian sigil = '\u26fc';
            Reactive(),
            Whitespace(),
            Name("asian"),
            Whitespace(),
            Name("sigil"),
            Whitespace(),
            Assign(),
            Whitespace(),
            Character("'\\u26fc'"),
            Terminal(),
            Whitespace(Environment.NewLine.Length + 4),

            // persistent birthday = 1976-01-23;
            Persistent(),
            Whitespace(),
            Name("birthday"),
            Whitespace(),
            Assign(),
            Whitespace(),
            Date(),
            Terminal(),
            Whitespace(Environment.NewLine.Length + 4),

            // shared hex = 0x2c;
            Shared(),
            Whitespace(),
            Name("hex"),
            Whitespace(),
            Assign(),
            Whitespace(),
            Hex("0x2c"),
            Terminal(),
            Whitespace(Environment.NewLine.Length + 4),

            // compiled dogs count = 7;
            Compiled(),
            Whitespace(),
            Name("dogs"),
            Whitespace(),
            Name("count"),
            Whitespace(),
            Assign(),
            Whitespace(),
            Integer("7"),
            Terminal(),
            Whitespace(Environment.NewLine.Length + 4),

            // var cash = $14.20;
            Variable(),
            Whitespace(),
            Name("cash"),
            Whitespace(),
            Assign(),
            Whitespace(),
            Money("$14.20"),
            Terminal(),
            Whitespace(Environment.NewLine.Length + 4),

            // var when = 17:24:24;
            Variable(),
            Whitespace(),
            Name("when"),
            Whitespace(),
            Assign(),
            Whitespace(),
            Time("17:24:24"),
            Terminal(),
            Whitespace(Environment.NewLine.Length + 4),

            // var googles address = https://google.com;
            Variable(),
            Whitespace(),
            Name("googles"),
            Whitespace(),
            Name("address"),
            Whitespace(),
            Assign(),
            Whitespace(),
            Url("https://google.com"),
            Terminal(),
            Whitespace(Environment.NewLine.Length),

            // }
            CloseBrace(),
            Whitespace(Environment.NewLine.Length * 2),

            // function run (list of stuff is integer[], things is number) away
            Function(),
            Whitespace(),
            Name("run"),
            Whitespace(),
            OpenParenthesis(),
            Name("list"),
            Whitespace(),
            Name("of"),
            Whitespace(),
            Name("stuff"),
            Whitespace(),
            Returns(),
            Whitespace(),
            Name("integer"),
            OpenSquareBracket(),
            CloseSquareBracket(),
            Separator(),
            Whitespace(),
            Name("things"),
            Whitespace(),
            Returns(),
            Whitespace(),
            Name("number"),
            CloseParenthesis(),
            Whitespace(),
            Name("away"),
            Whitespace(Environment.NewLine.Length),

            // {
            OpenBrace(),
            Whitespace(Environment.NewLine.Length + 4),

            // comment: // this assumes the list of stuff has at least one element
            Comment("// this assumes the list of stuff has at least one element"),
            Whitespace(Environment.NewLine.Length + 4),

            // return list of stuff[0] + things * 7;
            Name("return"),
            Whitespace(),
            Name("list"),
            Whitespace(),
            Name("of"),
            Whitespace(),
            Name("stuff"),
            OpenSquareBracket(),
            Integer("0"),
            CloseSquareBracket(),
            Whitespace(),
            Name("+"),
            Whitespace(),
            Name("things"),
            Whitespace(),
            Name("*"),
            Whitespace(),
            Integer("7"),
            Terminal(),
            Whitespace(Environment.NewLine.Length),

            // }
            CloseBrace(),

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
            if (i is 124)
            {
                int x = 45;
            }
            Assert.Equal(expected[i].GetType(), tokens[i].GetType());
            Assert.Equal(expected[i].Sourcecode.ToArray(), tokens[i].Sourcecode.ToArray());
        }

        Word Name(string name) => new(lexer, name.Length);
        Whitespace Whitespace(int spaces = 1) => new(lexer, spaces);
        Datatype Datatype() => new(lexer);
        Variable Variable() => new(lexer);
        Constant Constant() => new(lexer);
        Reactive Reactive() => new(lexer);
        Persistent Persistent() => new(lexer);
        Shared Shared() => new(lexer);
        Compiled Compiled() => new(lexer);
        Function Function() => new(lexer);
        Binary Binary(string value) => BinaryConstructor.Invoke(new object[] { lexer, value.Length }) as Binary;
        Character Character(string value) => CharacterConstructor.Invoke(new object[] { lexer, value.Length }) as Character;
        Date Date() => DateConstructor.Invoke(new object[] { lexer }) as Date;
        Hex Hex(string value) => HexConstructor.Invoke(new object[] { lexer, value.Length }) as Hex;
        Integer Integer(string value) => IntegerConstructor.Invoke(new object[] { lexer, value.Length }) as Integer;
        Money Money(string value) => MoneyConstructor.Invoke(new object[] { lexer, value.Length }) as Money;
        Time Time(string value) => TimeConstructor.Invoke(new object[] { lexer, value.Length }) as Time;
        Url Url(string value) => UrlConstructor.Invoke(new object[] { lexer, value.Length }) as Url;
        Comment Comment(string value) => CommentConstructor.Invoke(new object[] { lexer, value.Length }) as Comment;

        OpenBrace OpenBrace() => OpenBraceConstructor.Invoke(new object[] { lexer }) as OpenBrace;
        Assign Assign() => AssignConstructor.Invoke(new object[] { lexer }) as Assign;
        Terminal Terminal() => TerminalConstructor.Invoke(new object[] { lexer }) as Terminal;
        CloseBrace CloseBrace() => CloseBraceConstructor.Invoke(new object[] { lexer }) as CloseBrace;
        OpenParenthesis OpenParenthesis() => OpenParenthesisConstructor.Invoke(new object[] { lexer }) as OpenParenthesis;
        Returns Returns() => ReturnsConstructor.Invoke(new object[] { lexer }) as Returns;
        OpenSquareBracket OpenSquareBracket() => OpenSquareBracketConstructor.Invoke(new object[] { lexer }) as OpenSquareBracket;
        CloseSquareBracket CloseSquareBracket() => CloseSquareBracketConstructor.Invoke(new object[] { lexer }) as CloseSquareBracket;
        Separator Separator() => SeparatorConstructor.Invoke(new object[] { lexer }) as Separator;
        CloseParenthesis CloseParenthesis() => CloseParenthesisConstructor.Invoke(new object[] { lexer }) as CloseParenthesis;
    }

    private static ConstructorInfo GetConstructor<T>() => typeof(T).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(Ronin.Compiler.Lexer) });
    private static ConstructorInfo GetConstructor<T, TLength>() => typeof(T).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(Ronin.Compiler.Lexer), typeof(TLength) });

    private static readonly ConstructorInfo BinaryConstructor = GetConstructor<Binary, int>();
    private static readonly ConstructorInfo CharacterConstructor = GetConstructor<Character, int>();
    private static readonly ConstructorInfo DateConstructor = GetConstructor<Date>();
    private static readonly ConstructorInfo HexConstructor = GetConstructor<Hex, int>();
    private static readonly ConstructorInfo IntegerConstructor = GetConstructor<Integer, int>();
    private static readonly ConstructorInfo MoneyConstructor = GetConstructor<Money, int>();
    private static readonly ConstructorInfo TimeConstructor = GetConstructor<Time, int>();
    private static readonly ConstructorInfo UrlConstructor = GetConstructor<Url, int>();
    private static readonly ConstructorInfo CommentConstructor = GetConstructor<Comment, int>();

    private static readonly ConstructorInfo OpenBraceConstructor = GetConstructor<OpenBrace>();
    private static readonly ConstructorInfo AssignConstructor = GetConstructor<Assign>();
    private static readonly ConstructorInfo TerminalConstructor = GetConstructor<Terminal>();
    private static readonly ConstructorInfo CloseBraceConstructor = GetConstructor<CloseBrace>();
    private static readonly ConstructorInfo OpenParenthesisConstructor = GetConstructor<OpenParenthesis>();
    private static readonly ConstructorInfo ReturnsConstructor = GetConstructor<Returns>();
    private static readonly ConstructorInfo OpenSquareBracketConstructor = GetConstructor<OpenSquareBracket>();
    private static readonly ConstructorInfo CloseSquareBracketConstructor = GetConstructor<CloseSquareBracket>();
    private static readonly ConstructorInfo SeparatorConstructor = GetConstructor<Separator>();
    private static readonly ConstructorInfo CloseParenthesisConstructor = GetConstructor<CloseParenthesis>();

}
