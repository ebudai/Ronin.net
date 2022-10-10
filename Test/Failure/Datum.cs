using Ronin.Compiler;
using System.Xml.Linq;
using static Ronin.Grammar.Syntax;

namespace Failure;

public class Datum
{
    [Fact(DisplayName = "comments and whitespace")]
    public void CommentsAndWhitespace()
    {
        const string sourcecode = "  /* some comments */   ";

        Lexer lexer = new(sourcecode);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.IsNotType<Ronin.Grammar.Declaration.Datum>(syntax);
    }

    /*[Fact(DisplayName = "symbol before name")]
    public void SymbolBeforeName()
    {
        const string declaration = "reactive ( = 44.3;";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Language.Datum datum = new();

        var dequeued = tokens.TryDequeue(out var token);
        Assert.True(dequeued);
        datum.Add(token);

        dequeued = tokens.TryDequeue(out token);
        Assert.True(dequeued);
        datum.Add(token);

        dequeued = tokens.TryDequeue(out token);
        Assert.True(dequeued);
        var result = datum.Add(token);
        Assert.Equal(Result.DoesNotApply, result);
    }

    [Fact(DisplayName = "no datatype or initializer")]
    public void NoDatatypeOrInitializer()
    {
        const string declaration = "var x;";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Ronin.Language.Datum datum = new();

        var dequeued = tokens.TryDequeue(out var token);
        Assert.True(dequeued);
        datum.Add(token);

        dequeued = tokens.TryDequeue(out token);
        Assert.True(dequeued);
        datum.Add(token);

        dequeued = tokens.TryDequeue(out token);
        Assert.True(dequeued);
        datum.Add(token);

        dequeued = tokens.TryDequeue(out token);
        Assert.True(dequeued);
        var result = datum.Add(token);
        Assert.Equal(Result.DoesNotApply, result);
    }*/

    private static Ronin.Grammar.Declaration.Datum Compile(string declaration)
    {
        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Ronin.Grammar.Declaration.Datum>(syntax[0]);
        return syntax[0] as Ronin.Grammar.Declaration.Datum;
    }
}
