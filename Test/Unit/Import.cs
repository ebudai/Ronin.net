using Ronin.Grammar;

namespace Unit;

public class Import
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string line = "import standard/fun stuff/web sockets;";

        Ronin.Compiler.Lexer lexer = new(line);
        var tokens = lexer.Lex();
        Ronin.Grammar.Import import = new();
        while (tokens.TryDequeue(out var token))
        {
            var result = import.Add(token);

            if (result is Syntax.Result.Completed) break;
            if (result is not Syntax.Result.Applied) throw new Exception(Enum.GetName(result));
        }

        Assert.Empty(tokens);
        Assert.Equal(3, import.Name.Count);
        Assert.Equal("standard", import.Name[0]);
        Assert.Equal("fun stuff", import.Name[1]);
        Assert.Equal("web sockets", import.Name[2]);
    }

    [Fact(DisplayName = "keywords are just text")]
    public void WithKeywords()
    {
        const string line = "import return to whatever/secret/stuff;";

        Ronin.Compiler.Lexer lexer = new(line);
        var tokens = lexer.Lex();
        Ronin.Grammar.Import import = new();
        while (tokens.TryDequeue(out var token))
        {
            var result = import.Add(token);

            if (result is Syntax.Result.Completed) break;
            if (result is not Syntax.Result.Applied) throw new Exception(Enum.GetName(result));
        }

        Assert.Empty(tokens);
        Assert.Equal(3, import.Name.Count);
        Assert.Equal("return to whatever", import.Name[0]);
        Assert.Equal("secret", import.Name[1]);
        Assert.Equal("stuff", import.Name[2]);
    }
}