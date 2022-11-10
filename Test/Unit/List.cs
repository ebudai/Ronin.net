using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Grammar.Declaration;

namespace Unit;

public class List
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        const string declaration = "var x => integer[];";

        Lexer lexer = new(declaration);
        var tokens = lexer.Lex();
        Parser parser = new(tokens);
        var syntax = parser.Parse();

        Assert.NotEmpty(syntax);
        Assert.IsType<Statement>(syntax[0]);
        var statement = syntax[0] as Statement;
        Assert.NotNull(statement.DatumDeclaration);
        Assert.NotNull(statement.DatumDeclaration.Datatype);
        Assert.Empty(statement.DatumDeclaration.Datatype.Index.Values);
    }
}
