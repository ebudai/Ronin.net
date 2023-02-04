using Ronin.Compiler;
using Ronin.Lexicon;
using Ronin.Grammar;
using Ronin.Lexicon.Symbols;
using Test;
using Ronin.Lexicon.Keywords;

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
        tokens.Add<Ronin.Lexicon.Keywords.Datatype>()
            .Add<Word>("Test")
            .Add<OpenBrace>()
            .Add<CloseBrace>();
        
        Parser parser = new(tokens.ToArray());
        var datatype = Ronin.Grammar.Datatype.Parse(ref parser);
        
        Name name = datatype?.Identifier?.Components?[0];
        Assert.Equal("Test", name?.Words?[0]);
    }

    [Fact(DisplayName = "with algebra and members")]
    public void Algebra()
    {
        Tokens tokens = new();
        tokens.Add<Ronin.Lexicon.Keywords.Datatype>()
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
            .Add<CloseBrace>();

        Parser parser = new(tokens.ToArray());
        var datatype = Ronin.Grammar.Datatype.Parse(ref parser);

        Name algebra = datatype?.Algebra?.Components?[0];
        Assert.Equal("number or", string.Join(" ", algebra?.Words ?? new List<string>()));
        
        Datum cash = datatype?.Body?.Values?[0];
        Assert.IsType<Variable>(cash?.Mutability);
        Assert.Equal("cash", cash?.Name?.Words?[0]);
        Name cashtypename = cash?.Datatype?.Components?[0];
        Assert.Equal("money", cashtypename?.Words?[0]);

        Datum debt = datatype?.Body?.Values?[1];
        Assert.IsType<Variable>(debt?.Mutability);
        Assert.Equal("debt", debt?.Name?.Words?[0]);
        Name debttypename = debt?.Datatype?.Components?[0];
        Assert.Equal("money", debttypename?.Words?[0]);
    }
}
