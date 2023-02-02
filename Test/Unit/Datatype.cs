using Ronin.Compiler;
using Ronin.Grammar;
using Ronin.Lexicon;
using Ronin.Lexicon.Keywords;
using Ronin.Lexicon.Symbols;
using Test;

using DatatypeKeyword = Ronin.Lexicon.Keywords.Datatype;
using Datatype = Ronin.Grammar.Datatype;

namespace Unit;

[Trait("Parser", null)]
#pragma warning disable CS8981
#pragma warning disable IDE1006
public class datatype
{
    [Fact(DisplayName = "basic")]
    public void Basic()
    {
        Tokens tokens = new();
        tokens.Add<DatatypeKeyword>()
            .Add<Word>("Test")
            .Add<OpenBrace>()
            .Add<CloseBrace>()
            .Add(Sentinel.Instance);
        
        Parser parser = new(tokens.ToArray());
        var datatype = Datatype.Parse(ref parser);
        
        Assert.NotNull(datatype);
        Assert.NotNull(datatype.Identifier);
        Assert.Single(datatype.Identifier.Components);
        Name name = datatype.Identifier.Components[0];
        Assert.NotNull(name);
        Assert.Single(name.Words);
        Assert.Equal("Test", name.Words[0]);
    }

    [Fact(DisplayName = "with algebra and members")]
    public void Algebra()
    {
        Tokens tokens = new();
        tokens.Add<DatatypeKeyword>()
            .Add<Word>("Algebra")
            .Add<Assign>()
            .Add<Word>("number")
            .Add<Word>("or")
            .Add<OpenBrace>()
            .Add<Variable>()
            .Add<Word>("cash")
            .Add<Returns>()
            .Add<Word>("money")
            .Add<Terminal>()
            .Add<Variable>()
            .Add<Word>("debt")
            .Add<Returns>()
            .Add<Word>("money")
            .Add<Terminal>()
            .Add<CloseBrace>()
            .Add(Sentinel.Instance);

        Parser parser = new(tokens.ToArray());
        var datatype = Datatype.Parse(ref parser);

        Assert.NotNull(datatype);

        Assert.NotNull(datatype.Algebra);
        Assert.Single(datatype.Algebra.Components);
        Name algebra = datatype.Algebra.Components[0];
        Assert.NotNull(algebra);
        Assert.Equal(2, algebra.Words.Count);
        Assert.Equal("number", algebra.Words[0]);
        Assert.Equal("or", algebra.Words[1]);

        Assert.NotNull(datatype.Body);
        Assert.NotNull(datatype.Body.Values);
        Assert.Equal(2, datatype.Body.Values.Count);

        Datum cash = datatype.Body.Values[0];
        Assert.NotNull(cash);
        Assert.IsType<Variable>(cash.Mutability);
        Assert.Equal("cash", cash.Name.Words[0]);
        Assert.NotEmpty(cash.Datatype.Components);
        Name cashtypename = cash.Datatype.Components[0];
        Assert.Equal("money", string.Join(' ', cashtypename.Words));

        Datum debt = datatype.Body.Values[1];
        Assert.NotNull(debt);
        Assert.IsType<Variable>(debt.Mutability);
        Assert.Equal("debt", debt.Name.Words[0]);
        Assert.NotEmpty(debt.Datatype.Components);
        Name debttypename = debt.Datatype.Components[0];
        Assert.Equal("money", string.Join(' ', debttypename.Words));
    }
}
