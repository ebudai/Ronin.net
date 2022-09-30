using Ronin.Compiler;
using static Ronin.Grammar.Syntax;

namespace Failure;

/*public class Datum
{
    [Fact(DisplayName = "symbol before name")]
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
    }
}
*/