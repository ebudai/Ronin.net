using Ronin.Language;
using System.Reflection;
using Test;

namespace Unit;

[Trait("Analyzer", "declare")]
public class Function : AnalysisTests
{
    [Fact(DisplayName = "declare")]
    public void Declare()
    {
        // function thingy { }

        Ronin.Grammar.FunctionDeclaration declaration = new()
        {
            Name = Name("thingy"),
            Definition = new()
        };

        Ronin.Language.Function function = new(declaration, Context.Global);

        Assert.IsType<UnresolvedDatatype>(function.Returns);
        Assert.True(function.Definition.IsEmpty);
    }

    [Fact(DisplayName = "member function")]
    public void Member()
    {
        Ronin.Grammar.DatatypeDeclaration declaration = new()
        {
            Name = Name("thingy"),
            Definition = new()
            {
                Values = new() 
                { 
                    new Ronin.Grammar.FunctionDeclaration
                    {
                        Name = Name("method"),
                        Definition = new()
                    } 
                }
            }
        };

        Ronin.Language.Datatype type = new(declaration, Context.Global);

        var childrenGetter = typeof(Context).GetProperty("Contents", BindingFlags.Instance | BindingFlags.NonPublic);
        var children = childrenGetter.GetValue(type.Definition) as Dictionary<Ronin.Language.Identifier.Part, List<Semantic>>;
        Assert.Single(children);
        var semantics = children.ElementAt(0).Value;
        Assert.Single(semantics);
        var function = semantics[0] as Ronin.Language.Function;
        Assert.IsType<UnresolvedDatatype>(function?.Returns);
        Assert.True(function.Definition.IsEmpty);
    }
}
