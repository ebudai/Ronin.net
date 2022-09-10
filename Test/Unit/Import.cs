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
        var hierarchy = import.Name.Hierarchy;
        Assert.Equal(3, hierarchy.Count);
        Assert.Equal("standard", hierarchy[0]);
        Assert.Equal("fun stuff", hierarchy[1]);
        Assert.Equal("web sockets", hierarchy[2]);
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
        var hierarchy = import.Name.Hierarchy;
        Assert.Equal(3, hierarchy.Count);
        Assert.Equal("return to whatever", hierarchy[0]);
        Assert.Equal("secret", hierarchy[1]);
        Assert.Equal("stuff", hierarchy[2]);
    }

    [Fact(DisplayName = "uses url")]
    public void Url()
    {
        const string line = "import git://github.com/ebudai/ronin.git;";

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
        Assert.Equal("git://github.com/ebudai/ronin.git", import.Url.Sourcecode.ToString());
    }
}