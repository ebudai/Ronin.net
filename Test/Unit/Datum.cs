using Ronin.Grammar;
using Ronin.Language;
using Ronin.Lexicon.Keywords;
using System.Buffers;
using Test;

namespace Unit;

[Trait("Analyzer", "declare")]
public class Datum : AnalysisTests
{
    [Fact(DisplayName = "variable")]
    public void Variable()
    {
        const string threedollars = "$3";

        // var x = $3;

        Ronin.Lexicon.Literal literal = new();
        literal.SetMemory(threedollars.ToArray());
        Value initializer = new Literal { Source = new[] { literal } };

        Ronin.Grammar.DatumDeclaration declaration = new()
        {
            Mutability = new Variable(),
            Modifiers = new(),
            Name = Name("x"),
            Initializer = initializer
        };

        Ronin.Language.Datum datum = new(declaration, Context.Global);

        Assert.Equal(Mutability.Variable, datum?.Mutability);
        
        Assert.False(datum.IsCompiled);
        Assert.False(datum.IsShared);
        Assert.False(datum.IsPersistent);

        Assert.IsType<UnresolvedDatatype>(datum.Datatype);

        Literal value = datum.Initializer;
        Assert.Equal(1, value.Source.Length);
        Assert.Equal(threedollars, value.Source.Span[0].Memory.ToString());
    }

    [Fact(DisplayName = "reactive")]
    public void Reactive()
    {
        // reactive x = y * 3;

        //Value initializer = new Reference { Components =  };

        Ronin.Grammar.DatumDeclaration declaration = new()
        {
            Mutability = new Reactive(),
            Modifiers = new(),
            Name = Name("x"),
            //Initializer = initializer
        };

        Ronin.Language.Datum datum = new(declaration, Context.Global);

        Assert.Equal(Mutability.Reactive, datum?.Mutability);

        Assert.False(datum.IsCompiled);
        Assert.False(datum.IsShared);
        Assert.False(datum.IsPersistent);

        Assert.IsType<UnresolvedDatatype>(datum.Datatype);
    }
}
