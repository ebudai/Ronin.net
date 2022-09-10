using Ronin.Grammar;

namespace Unit;

public class PartOf
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string line = "part of standard/fun stuff/web sockets;";

        Ronin.Compiler.Lexer lexer = new(line);
        var tokens = lexer.Lex();
        Ronin.Grammar.PartOf partof = new();
        while (tokens.TryDequeue(out var token))
        {
            var result = partof.Add(token);

            if (result is Syntax.Result.Completed) break;
            if (result is not Syntax.Result.Applied) throw new Exception(Enum.GetName(result));
        }

        Assert.Empty(tokens);
        var hierarchy = partof.Name.Hierarchy;
        Assert.Equal(3, hierarchy.Count);
        Assert.Equal("standard", hierarchy[0]);
        Assert.Equal("fun stuff", hierarchy[1]);
        Assert.Equal("web sockets", hierarchy[2]);
    }

    [Fact(DisplayName = "keywords are just text")]
    public void WithKeywords()
    {
        const string line = "part of return to whatever/secret/stuff;";

        Ronin.Compiler.Lexer lexer = new(line);
        var tokens = lexer.Lex();
        Ronin.Grammar.PartOf partof = new();
        while (tokens.TryDequeue(out var token))
        {
            var result = partof.Add(token);

            if (result is Syntax.Result.Completed) break;
            if (result is not Syntax.Result.Applied) throw new Exception(Enum.GetName(result));
        }

        Assert.Empty(tokens);
        var hierarchy = partof.Name.Hierarchy;
        Assert.Equal(3, hierarchy.Count);
        Assert.Equal("return to whatever", hierarchy[0]);
        Assert.Equal("secret", hierarchy[1]);
        Assert.Equal("stuff", hierarchy[2]);
    }
}