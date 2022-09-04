using Ronin.Tokens;

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
            var sigil = 'c';
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
            new Ronin.Tokens.Modifiers.Datatype(lexer),
            new Ronin.Tokens.Whitespace(lexer, 1),
            Name(lexer, "Weather"),
            new Ronin.Tokens.Whitespace(lexer, 1),
            new Ronin.Tokens.Symbols.OpenBrace(lexer),
            new Ronin.Tokens.Whitespace(lexer, 2),
            new Ronin.Tokens.Modifiers.Variable(lexer),
            new Ronin.Tokens.Whitespace(lexer, 1),
            Name(lexer, "data"),
            new Ronin.Tokens.Whitespace(lexer, 1),
            Name(lexer, "="),
            new Ronin.Tokens.Whitespace(lexer, 1),
            //new Ronin.Tokens.Literals.BinaryLiteral(lexer, )
        };
        
        

        var tokens = lexer.Lex();


        static Name Name(Ronin.Compiler.Lexer lexer, string name) => new(lexer, name.Length);

        int i = 0;
        Assert.IsType<Ronin.Tokens.Modifiers.Datatype>(tokens[i++]);
        
        Assert.IsType<Ronin.Tokens.Whitespace>(tokens[i++]);
        
        Assert.IsType<Name>(tokens[i]);
        Assert.Equal("Weather", tokens[i++].Sourcecode.ToString());

        Assert.IsType<Ronin.Tokens.Whitespace>(tokens[i++]);

        Assert.IsType<Ronin.Tokens.Symbols.OpenBrace>(tokens[i++]);

        Assert.IsType<Ronin.Tokens.Whitespace>(tokens[i++]);

        Assert.IsType<Ronin.Tokens.Modifiers.Variable>(tokens[i++]);

        Assert.IsType<Ronin.Tokens.Whitespace>(tokens[i++]);

        Assert.IsType<Name>(tokens[i]);
        Assert.Equal("data", tokens[i++].Sourcecode.ToString());

        Assert.IsType<Ronin.Tokens.Whitespace>(tokens[i++]);

        Assert.IsType<Name>(tokens[i]);
        Assert.Equal("=", tokens[i++].Sourcecode.ToString());

        Assert.IsType<Ronin.Tokens.Whitespace>(tokens[i++]);

        Assert.IsType<Ronin.Tokens.Literals.BinaryLiteral>(tokens[i]);
        Assert.Equal("0b01101010010", tokens[i++].Sourcecode.ToString());

        Assert.IsType<Ronin.Tokens.Symbols.Terminal>(tokens[i++]);

        Assert.IsType<Ronin.Tokens.Whitespace>(tokens[i++]);

        Assert.IsType<Ronin.Tokens.Modifiers.Variable>(tokens[i++]);

        Assert.IsType<Ronin.Tokens.Whitespace>(tokens[i++]);

        Assert.IsType<Name>(tokens[i]);
        Assert.Equal("sigil", tokens[i++].Sourcecode.ToString());

        Assert.IsType<Ronin.Tokens.Whitespace>(tokens[i++]);

        Assert.IsType<Name>(tokens[i]);
        Assert.Equal("=", tokens[i++].Sourcecode.ToString());

        Assert.IsType<Ronin.Tokens.Whitespace>(tokens[i++]);

        Assert.IsType<Ronin.Tokens.Literals.CharLiteral>(tokens[i]);
        Assert.Equal("'c'", tokens[i++].Sourcecode.ToString());

        Assert.IsType<Ronin.Tokens.Symbols.Terminal>(tokens[i++]);

        Assert.IsType<Ronin.Tokens.Whitespace>(tokens[i++]);

        Assert.IsType<Ronin.Tokens.Modifiers.Variable>(tokens[i++]);

        Assert.IsType<Ronin.Tokens.Whitespace>(tokens[i++]);

        Assert.IsType<Name>(tokens[i]);
        Assert.Equal("asian", tokens[i++].Sourcecode.ToString());

        Assert.IsType<Ronin.Tokens.Whitespace>(tokens[i++]);

        Assert.IsType<Name>(tokens[i]);
        Assert.Equal("sigil", tokens[i++].Sourcecode.ToString());

        Assert.IsType<Ronin.Tokens.Whitespace>(tokens[i++]);

        Assert.IsType<Name>(tokens[i]);
        Assert.Equal("=", tokens[i++].Sourcecode.ToString());

        Assert.IsType<Ronin.Tokens.Whitespace>(tokens[i++]);

        Assert.IsType<Ronin.Tokens.Literals.CharLiteral>(tokens[i]);
        Assert.Equal("'\u26fc'", tokens[i++].Sourcecode.ToString());

        Assert.IsType<Ronin.Tokens.Symbols.Terminal>(tokens[i++]);

        Assert.IsType<Ronin.Tokens.Whitespace>(tokens[i++]);

        Assert.IsType<Ronin.Tokens.Modifiers.Variable>(tokens[i++]);

        Assert.IsType<Ronin.Tokens.Whitespace>(tokens[i++]);

        Assert.IsType<Name>(tokens[i]);
        Assert.Equal("birthday", tokens[i++].Sourcecode.ToString());

        Assert.IsType<Ronin.Tokens.Whitespace>(tokens[i++]);


    }
}
